using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.Models;

namespace UniKnowledge.Hubs;

public class UserPresenceDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
}

public class CursorPositionDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int Column { get; set; }
    public string? ConnectionId { get; set; }
}

[Authorize]
public class CollaborativeCodeHub : Hub
{
    // Enterprise-Grade Thread-Safety: Nested ConcurrentDictionary prevents race conditions and lock contentions.
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, UserPresenceDto>> _presenceMap = new();

    public async Task JoinRoom(string roomId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        // Optimization: Retrieve Username, FullName, and AvatarUrl directly from JWT Claims to avoid Database I/O bottlenecks.
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
        var fullName = Context.User?.FindFirst("FullName")?.Value ?? Context.User?.FindFirst(ClaimTypes.GivenName)?.Value;
        var avatarUrl = Context.User?.FindFirst("AvatarUrl")?.Value;

        var connectionId = Context.ConnectionId;
        var groupName = $"code_room_{roomId}";

        // Add client connection to the SignalR group
        await Groups.AddToGroupAsync(connectionId, groupName);

        var roomPresence = _presenceMap.GetOrAdd(roomId, _ => new ConcurrentDictionary<string, UserPresenceDto>());
        var presenceDto = new UserPresenceDto
        {
            UserId = userId.Value,
            Username = username,
            FullName = fullName,
            AvatarUrl = avatarUrl,
            ConnectionId = connectionId
        };

        // Atomic assignment prevents race conditions between simultaneous joining connections
        roomPresence[connectionId] = presenceDto;

        var currentList = roomPresence.Values.ToList();

        // Broadcast updated presence list to everyone else in the group
        await Clients.OthersInGroup(groupName).SendAsync("UserPresenceChanged", currentList);
        
        // Also send presence list to the joining user so they know who is currently in the room
        await Clients.Caller.SendAsync("UserPresenceChanged", currentList);
    }

    public async Task LeaveRoom(string roomId)
    {
        var connectionId = Context.ConnectionId;
        var groupName = $"code_room_{roomId}";

        // Remove from SignalR group
        await Groups.RemoveFromGroupAsync(connectionId, groupName);

        if (_presenceMap.TryGetValue(roomId, out var roomPresence))
        {
            // Atomically remove connection from the room presence map
            roomPresence.TryRemove(connectionId, out _);
            
            // Lock-Free Optimization: Remove the room entirely if presence list becomes empty
            if (roomPresence.IsEmpty)
            {
                _presenceMap.TryRemove(KeyValuePair.Create(roomId, roomPresence));
                // Optimization: Do NOT broadcast presence updates to a completely empty group.
            }
            else
            {
                var currentList = roomPresence.Values.ToList();
                // Broadcast updated presence list only if there are remaining members in the room
                await Clients.Group(groupName).SendAsync("UserPresenceChanged", currentList);
            }
        }
    }

    public async Task SendCodeDelta(string roomId, object deltas)
    {
        var groupName = $"code_room_{roomId}";
        // Broadcast code delta to others in the group
        await Clients.OthersInGroup(groupName).SendAsync("ReceiveCodeDelta", deltas);
    }

    public async Task SendCursorPosition(string roomId, CursorPositionDto cursorData)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
        
        cursorData.ConnectionId = Context.ConnectionId;
        cursorData.UserId = userId.Value;
        cursorData.Username = username;

        var groupName = $"code_room_{roomId}";
        // Broadcast remote cursor movements to others in the group
        await Clients.OthersInGroup(groupName).SendAsync("ReceiveCursorMoved", cursorData);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var roomsToNotify = new List<(string RoomId, List<UserPresenceDto> UpdatedList)>();

            foreach (var key in _presenceMap.Keys)
            {
                if (_presenceMap.TryGetValue(key, out var roomPresence))
                {
                    // Atomically remove connection and verify if it was present
                    if (roomPresence.TryRemove(connectionId, out _))
                    {
                        // Lock-Free Optimization: Remove key from the static map if it became empty
                        if (roomPresence.IsEmpty)
                        {
                            _presenceMap.TryRemove(KeyValuePair.Create(key, roomPresence));
                            // Optimization: Do NOT queue empty rooms for broadcase notifications.
                        }
                        else
                        {
                            var updatedList = roomPresence.Values.ToList();
                            roomsToNotify.Add((key, updatedList));
                        }
                    }
                }
            }

            foreach (var room in roomsToNotify)
            {
                var groupName = $"code_room_{room.RoomId}";
                await Groups.RemoveFromGroupAsync(connectionId, groupName);
                await Clients.Group(groupName).SendAsync("UserPresenceChanged", room.UpdatedList);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CollaborativeCodeHub.OnDisconnectedAsync: {ex.Message}");
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    private int? GetUserId()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = Context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            return userId;

        return null;
    }
}

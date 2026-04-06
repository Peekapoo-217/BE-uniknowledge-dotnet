using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Message;
using UniKnowledge.DTOs.Shared;
using UniKnowledge.Helpers;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public interface IMessageService
{
    Task<MessageResponseDto> CreateMessageAsync(int senderId, int receiverId, string content);
    Task<CursorPagedResult<MessageResponseDto>> GetConversationAsync(int userId, int otherUserId, int limit = 50, string? after = null);
    Task<MessageResponseDto?> MarkAsReadAsync(int messageId, int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<CursorPagedResult<ConversationDto>> GetConversationsAsync(int userId, int limit = 20, string? after = null);
}

public class MessageService : IMessageService
{
    private readonly AppDbContext _context;

    public MessageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MessageResponseDto> CreateMessageAsync(int senderId, int receiverId, string content)
    {
        // Validate users exist
        var sender = await _context.Users.FindAsync(senderId);
        var receiver = await _context.Users.FindAsync(receiverId);

        if (sender == null || receiver == null)
        {
            throw new Exception("User not found");
        }

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return MapToDto(message, sender, receiver);
    }

    public async Task<CursorPagedResult<MessageResponseDto>> GetConversationAsync(int userId, int otherUserId, int limit = 50, string? after = null)
    {
        var query = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                       (m.SenderId == otherUserId && m.ReceiverId == userId));

        // Apply cursor (load older messages)
        var cursor = CursorHelper.Decode(after);
        if (cursor.HasValue)
        {
            var (cursorDate, cursorId) = cursor.Value;
            query = query.Where(m => m.CreatedAt < cursorDate ||
                        (m.CreatedAt == cursorDate && m.MessageId < cursorId));
        }

        // Get newest first, then reverse for display
        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.MessageId)
            .Take(limit + 1)
            .ToListAsync();

        var hasNextPage = messages.Count > limit;
        var items = messages.Take(limit).Reverse().ToList(); // Reverse for chronological order

        var dtos = items.Select(m => MapToDto(m, m.Sender, m.Receiver)).ToList();

        return new CursorPagedResult<MessageResponseDto>
        {
            Items = dtos,
            PageInfo = new PageInfo
            {
                HasNextPage = hasNextPage,
                // EndCursor points to the oldest message in this batch (for loading more older messages)
                EndCursor = messages.Take(limit).Any()
                    ? CursorHelper.Encode(messages.Take(limit).Last().CreatedAt, messages.Take(limit).Last().MessageId)
                    : null
            }
        };
    }

    public async Task<MessageResponseDto?> MarkAsReadAsync(int messageId, int userId)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstOrDefaultAsync(m => m.MessageId == messageId && m.ReceiverId == userId);

        if (message == null)
        {
            return null;
        }

        if (!message.IsRead)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return MapToDto(message, message.Sender, message.Receiver);
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Messages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
    }

    public async Task<CursorPagedResult<ConversationDto>> GetConversationsAsync(int userId, int limit = 20, string? after = null)
    {
        // Step 1: Get distinct conversation partner IDs (1 query, uses index)
        var partnerIds = await _context.Messages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Distinct()
            .ToListAsync();

        if (!partnerIds.Any())
        {
            return new CursorPagedResult<ConversationDto>
            {
                Items = new List<ConversationDto>(),
                PageInfo = new PageInfo { HasNextPage = false }
            };
        }

        // Step 2: Batch load all partner users (1 query instead of N FindAsync calls)
        var partners = await _context.Users
            .Where(u => partnerIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        // Step 3: Build conversations with last message + unread count per partner
        // Each query is O(log N) thanks to composite indexes
        var allConversations = new List<ConversationDto>();

        foreach (var partnerId in partnerIds)
        {
            if (!partners.ContainsKey(partnerId)) continue;
            var partner = partners[partnerId];

            var lastMessage = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == partnerId) ||
                           (m.SenderId == partnerId && m.ReceiverId == userId))
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            var unreadCount = await _context.Messages
                .CountAsync(m => m.SenderId == partnerId && m.ReceiverId == userId && !m.IsRead);

            allConversations.Add(new ConversationDto
            {
                OtherUserId = partnerId,
                OtherUsername = partner.Username,
                OtherAvatarUrl = partner.AvatarUrl,
                LastMessage = lastMessage?.Content,
                LastMessageTime = lastMessage?.CreatedAt,
                UnreadCount = unreadCount,
                IsLastMessageFromMe = lastMessage?.SenderId == userId
            });
        }

        // Sort by last message time (most recent first)
        allConversations = allConversations
            .OrderByDescending(c => c.LastMessageTime ?? DateTime.MinValue)
            .ToList();

        // Apply cursor
        var startIndex = 0;
        if (!string.IsNullOrEmpty(after))
        {
            var cursorData = CursorHelper.Decode(after);
            if (cursorData.HasValue)
            {
                var (cursorDate, cursorId) = cursorData.Value;
                startIndex = allConversations.FindIndex(c =>
                    c.LastMessageTime < cursorDate ||
                    (c.LastMessageTime == cursorDate && c.OtherUserId < cursorId));
                if (startIndex < 0) startIndex = allConversations.Count;
            }
        }

        var paged = allConversations.Skip(startIndex).Take(limit + 1).ToList();
        var hasNextPage = paged.Count > limit;
        var items = paged.Take(limit).ToList();

        return new CursorPagedResult<ConversationDto>
        {
            Items = items,
            PageInfo = new PageInfo
            {
                HasNextPage = hasNextPage,
                EndCursor = items.Any() && items.Last().LastMessageTime.HasValue
                    ? CursorHelper.Encode(items.Last().LastMessageTime!.Value, items.Last().OtherUserId)
                    : null
            }
        };
    }

    private MessageResponseDto MapToDto(Message message, User sender, User receiver)
    {
        return new MessageResponseDto
        {
            MessageId = message.MessageId,
            SenderId = message.SenderId,
            SenderUsername = sender.Username,
            SenderAvatarUrl = sender.AvatarUrl,
            ReceiverId = message.ReceiverId,
            ReceiverUsername = receiver.Username,
            Content = message.Content,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt
        };
    }
}


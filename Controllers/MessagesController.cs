using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniKnowledge.DTOs.Message;
using UniKnowledge.Services;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult> GetConversations([FromQuery] int limit = 20, [FromQuery] string? after = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var conversations = await _messageService.GetConversationsAsync(userId, limit, after);
        return Ok(conversations);
    }

    [HttpGet("conversation/{otherUserId}")]
    public async Task<ActionResult> GetConversation(
        int otherUserId,
        [FromQuery] int limit = 50,
        [FromQuery] string? after = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var messages = await _messageService.GetConversationAsync(userId, otherUserId, limit, after);
        return Ok(messages);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var count = await _messageService.GetUnreadCountAsync(userId);
        return Ok(new { unreadCount = count });
    }

    [HttpPost]
    public async Task<ActionResult<MessageResponseDto>> CreateMessage([FromBody] CreateMessageDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        if (userId == dto.ReceiverId)
        {
            return BadRequest(new { message = "Cannot send message to yourself" });
        }

        try
        {
            var message = await _messageService.CreateMessageAsync(userId, dto.ReceiverId, dto.Content);
            return Ok(message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{messageId}/read")]
    public async Task<ActionResult<MessageResponseDto>> MarkAsRead(int messageId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var message = await _messageService.MarkAsReadAsync(messageId, userId);

        if (message == null)
        {
            return NotFound(new { message = "Message not found or unauthorized" });
        }

        return Ok(message);
    }
}


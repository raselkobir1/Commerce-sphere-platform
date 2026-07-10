using CommerceSphere.ChatService.API.Auth;
using CommerceSphere.ChatService.Application.DTOs.Requests;
using CommerceSphere.ChatService.Application.Interfaces;
using CommerceSphere.Shared.Common.Correlation;
using CommerceSphere.Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommerceSphere.ChatService.API.Controllers;

// REST surface for live support chat. Messages are sent here (persisted first, then broadcast over
// SignalR by the ChatManager), and history/conversation lists are read here. The SignalR hub
// (/hubs/chat) is only the real-time delivery channel.
[ApiController]
[Route("api/chat")]
[Produces("application/json")]
[Authorize]
public class ChatController(IChatManager manager) : ControllerBase
{
    // Customer: fetch (or lazily create) my own support conversation.
    [HttpGet("conversations/me")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyConversation(CancellationToken ct)
    {
        var result = await manager.GetOrCreateMyConversationAsync(User.ToChatUser(), ct);
        return Ok(ApiResponse<object>.Ok(result, "Conversation ready",
            HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    // Agent inbox: all conversations, newest activity first.
    [HttpGet("conversations")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        var result = await manager.GetConversationsAsync(ct);
        return Ok(ApiResponse<object>.Ok(result, "Conversations retrieved",
            HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    // Message history for a conversation (customer: own only; agent: any).
    [HttpGet("conversations/{id:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct)
    {
        var result = await manager.GetMessagesAsync(id, User.ToChatUser(), ct);
        return Ok(ApiResponse<object>.Ok(result, "Messages retrieved",
            HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }

    // Send a message into a conversation. Persisted, then pushed in real time to the other party.
    [HttpPost("conversations/{id:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var result = await manager.SendMessageAsync(id, User.ToChatUser(), request, ct);
        return Ok(ApiResponse<object>.Ok(result, "Message sent",
            HttpContext.TraceIdentifier, HttpContext.GetCorrelationId()));
    }
}

using CommerceSphere.ChatService.API.Auth;
using CommerceSphere.ChatService.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CommerceSphere.ChatService.API.Realtime;

// Real-time transport for live support chat. Authenticated over JWT (passed as ?access_token= on the
// WebSocket handshake — see Program.cs). Delivery is group-based:
//   • group "conv:{conversationId}"  — the customer + any agent viewing that thread
//   • group "support"                — all connected agents (for the inbox live updates)
// Clients call JoinConversation to subscribe to a thread; the server pushes "ReceiveMessage" and
// "ConversationUpdated" (from SignalRChatNotifier). Messages are sent/persisted over REST, not here,
// so a message is always durable before it is broadcast.
[Authorize]
public class ChatHub(IConversationRepository conversations) : Hub
{
    public const string SupportGroup = "support";
    public static string ConversationGroup(Guid conversationId) => $"conv:{conversationId}";

    public override async Task OnConnectedAsync()
    {
        // Agents watch the whole inbox.
        if (Context.User!.ToChatUser().IsSupport)
            await Groups.AddToGroupAsync(Context.ConnectionId, SupportGroup);

        await base.OnConnectedAsync();
    }

    // Subscribe this connection to a conversation's live feed. A customer may only join their own
    // thread; an agent may join any. Prevents eavesdropping by guessing another conversation's id.
    public async Task JoinConversation(Guid conversationId)
    {
        var caller = Context.User!.ToChatUser();

        var conversation = await conversations.GetByIdAsync(conversationId, Context.ConnectionAborted)
            ?? throw new HubException("Conversation not found.");

        if (!caller.IsSupport && conversation.CustomerId != caller.UserId)
            throw new HubException("You do not have access to this conversation.");

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
}

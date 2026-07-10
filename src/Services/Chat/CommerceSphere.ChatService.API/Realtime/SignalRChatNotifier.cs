using CommerceSphere.ChatService.Application.DTOs.Responses;
using CommerceSphere.ChatService.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CommerceSphere.ChatService.API.Realtime;

// SignalR implementation of the Application's IChatNotifier abstraction.
// New messages go to the conversation group (customer + agent viewing it); conversation state
// changes go to the support group so every agent's inbox updates live.
public class SignalRChatNotifier(IHubContext<ChatHub> hub) : IChatNotifier
{
    public Task MessageSentAsync(ChatMessageResponse message, CancellationToken ct = default) =>
        hub.Clients.Group(ChatHub.ConversationGroup(message.ConversationId))
            .SendAsync("ReceiveMessage", message, ct);

    public Task ConversationUpdatedAsync(ConversationResponse conversation, CancellationToken ct = default) =>
        hub.Clients.Group(ChatHub.SupportGroup)
            .SendAsync("ConversationUpdated", conversation, ct);
}

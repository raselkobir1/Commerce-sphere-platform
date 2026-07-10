using CommerceSphere.ChatService.Application.DTOs.Responses;

namespace CommerceSphere.ChatService.Application.Interfaces;

// Pushes chat events to connected clients in real time. Implemented in the API layer over SignalR;
// abstracted here so the Application layer (ChatManager) stays transport-agnostic.
public interface IChatNotifier
{
    // A new message was added — delivered to everyone in that conversation (the customer and any
    // agent who has the thread open).
    Task MessageSentAsync(ChatMessageResponse message, CancellationToken ct = default);

    // A conversation's state changed (new message / unread count) — delivered to the agent inbox
    // so the list can reorder and update its unread badge without a full refresh.
    Task ConversationUpdatedAsync(ConversationResponse conversation, CancellationToken ct = default);
}

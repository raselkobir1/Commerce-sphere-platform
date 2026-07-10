using CommerceSphere.ChatService.Application.DTOs.Requests;
using CommerceSphere.ChatService.Application.DTOs.Responses;
using CommerceSphere.ChatService.Application.Interfaces;
using CommerceSphere.ChatService.Domain.Entities;
using CommerceSphere.ChatService.Domain.Interfaces;
using CommerceSphere.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.ChatService.Application.Managers;

// Use cases for live support chat: resolve/create the customer's thread, list threads for agents,
// read history, and post messages. After persisting a message it is pushed to connected clients
// in real time via IChatNotifier (implemented over SignalR in the API layer).
public class ChatManager(
    IUnitOfWork uow,
    IChatNotifier notifier,
    ILogger<ChatManager> logger) : IChatManager
{
    private const int MaxMessageLength = 2000;

    public async Task<ConversationResponse> GetOrCreateMyConversationAsync(ChatUser customer, CancellationToken ct = default)
    {
        var conversation = await uow.Conversations.GetByCustomerIdAsync(customer.UserId, ct);
        if (conversation is null)
        {
            conversation = Conversation.Start(customer.UserId, customer.Name, customer.Email);
            await uow.Conversations.AddAsync(conversation, ct);
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Started support conversation {ConversationId} for customer {CustomerId}",
                conversation.Id, customer.UserId);
        }

        return Map(conversation);
    }

    public async Task<IReadOnlyList<ConversationResponse>> GetConversationsAsync(CancellationToken ct = default)
    {
        var conversations = await uow.Conversations.GetAllOrderedAsync(ct);
        return conversations.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(
        Guid conversationId, ChatUser caller, CancellationToken ct = default)
    {
        var conversation = await ResolveAccessibleConversationAsync(conversationId, caller, ct);

        // An agent opening the thread has now seen the customer's messages — clear the unread badge.
        if (caller.IsSupport && conversation.UnreadForSupport > 0)
        {
            conversation.MarkReadBySupport();
            uow.Conversations.Update(conversation);
            await uow.SaveChangesAsync(ct);
            await notifier.ConversationUpdatedAsync(Map(conversation), ct);
        }

        var messages = await uow.Messages.GetByConversationAsync(conversationId, ct);
        return messages.Select(Map).ToList();
    }

    public async Task<ChatMessageResponse> SendMessageAsync(
        Guid conversationId, ChatUser caller, SendMessageRequest request, CancellationToken ct = default)
    {
        var content = (request.Content ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(content))
            throw new BusinessException("Message cannot be empty.");
        if (content.Length > MaxMessageLength)
            throw new BusinessException($"Message is too long (max {MaxMessageLength} characters).");

        var conversation = await ResolveAccessibleConversationAsync(conversationId, caller, ct);

        var senderRole = caller.IsSupport ? SenderRole.Support : SenderRole.Customer;
        var message = ChatMessage.Create(conversation.Id, caller.UserId, senderRole, caller.Name, content);
        await uow.Messages.AddAsync(message, ct);

        conversation.RecordMessage(content, fromCustomer: !caller.IsSupport);
        uow.Conversations.Update(conversation);

        await uow.SaveChangesAsync(ct);

        // Push in real time only after the write is durable, so a delivered message always persisted.
        var messageDto = Map(message);
        await notifier.MessageSentAsync(messageDto, ct);
        await notifier.ConversationUpdatedAsync(Map(conversation), ct);

        logger.LogInformation("Message {MessageId} sent to conversation {ConversationId} by {Role}",
            message.Id, conversation.Id, senderRole);

        return messageDto;
    }

    // Loads the conversation and enforces access: a customer may only touch their own thread
    // (we return 404 to avoid revealing that another customer's thread exists); an agent may touch any.
    private async Task<Conversation> ResolveAccessibleConversationAsync(
        Guid conversationId, ChatUser caller, CancellationToken ct)
    {
        var conversation = await uow.Conversations.GetByIdAsync(conversationId, ct)
            ?? throw new NotFoundException(nameof(Conversation), conversationId);

        if (!caller.IsSupport && conversation.CustomerId != caller.UserId)
            throw new NotFoundException(nameof(Conversation), conversationId);

        return conversation;
    }

    private static ConversationResponse Map(Conversation c) =>
        new(c.Id, c.CustomerId, c.CustomerName, c.CustomerEmail,
            c.LastMessagePreview, c.LastMessageAt, c.UnreadForSupport);

    private static ChatMessageResponse Map(ChatMessage m) =>
        new(m.Id, m.ConversationId, m.SenderId, m.SenderRole, m.SenderName, m.Content, m.CreatedAt);
}

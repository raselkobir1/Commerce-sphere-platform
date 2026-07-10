using CommerceSphere.ChatService.Domain.Entities;

namespace CommerceSphere.ChatService.Domain.Interfaces.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // A customer has at most one support conversation — looked up by their user id.
    Task<Conversation?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);

    // Newest-activity-first list for the agent inbox.
    Task<IReadOnlyList<Conversation>> GetAllOrderedAsync(CancellationToken ct = default);

    Task AddAsync(Conversation conversation, CancellationToken ct = default);

    void Update(Conversation conversation);
}

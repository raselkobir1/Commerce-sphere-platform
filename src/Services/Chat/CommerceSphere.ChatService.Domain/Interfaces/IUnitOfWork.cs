using CommerceSphere.ChatService.Domain.Interfaces.Repositories;

namespace CommerceSphere.ChatService.Domain.Interfaces;

public interface IUnitOfWork
{
    IConversationRepository Conversations { get; }
    IChatMessageRepository Messages { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

using CommerceSphere.ChatService.Domain.Interfaces;
using CommerceSphere.ChatService.Domain.Interfaces.Repositories;
using CommerceSphere.ChatService.Infrastructure.Data;

namespace CommerceSphere.ChatService.Infrastructure.UnitOfWork;

public class UnitOfWork(
    ChatDbContext db,
    IConversationRepository conversations,
    IChatMessageRepository messages) : IUnitOfWork
{
    public IConversationRepository Conversations { get; } = conversations;
    public IChatMessageRepository Messages { get; } = messages;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

using CommerceSphere.NotificationService.Domain.Entities;

namespace CommerceSphere.NotificationService.Domain.Interfaces.Repositories;

public interface IInboxRepository
{
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task AddAsync(InboxMessage message, CancellationToken ct = default);
}

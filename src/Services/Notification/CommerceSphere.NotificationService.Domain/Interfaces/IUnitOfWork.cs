using CommerceSphere.NotificationService.Domain.Interfaces.Repositories;

namespace CommerceSphere.NotificationService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    INotificationRepository Notifications { get; }
    IInboxRepository Inbox { get; }
    IUserContactRepository Contacts { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

using CommerceSphere.NotificationService.Domain.Interfaces;
using CommerceSphere.NotificationService.Domain.Interfaces.Repositories;
using CommerceSphere.NotificationService.Infrastructure.Data;
using CommerceSphere.NotificationService.Infrastructure.Repositories;

namespace CommerceSphere.NotificationService.Infrastructure.UnitOfWork;

public class UnitOfWork(NotificationDbContext db) : IUnitOfWork
{
    private INotificationRepository? _notifications;
    private IInboxRepository? _inbox;
    private IUserContactRepository? _contacts;

    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(db);
    public IInboxRepository Inbox => _inbox ??= new InboxRepository(db);
    public IUserContactRepository Contacts => _contacts ??= new UserContactRepository(db);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public void Dispose() => db.Dispose();
}

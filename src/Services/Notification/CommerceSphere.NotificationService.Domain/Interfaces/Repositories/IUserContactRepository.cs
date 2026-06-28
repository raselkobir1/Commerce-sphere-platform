using CommerceSphere.NotificationService.Domain.Entities;

namespace CommerceSphere.NotificationService.Domain.Interfaces.Repositories;

public interface IUserContactRepository
{
    Task<UserContact?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserContact contact, CancellationToken ct = default);
    void Update(UserContact contact);
}

using CommerceSphere.NotificationService.Domain.Entities;
using CommerceSphere.NotificationService.Domain.Interfaces.Repositories;
using CommerceSphere.NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommerceSphere.NotificationService.Infrastructure.Repositories;

public class UserContactRepository(NotificationDbContext db) : IUserContactRepository
{
    public Task<UserContact?> GetByIdAsync(Guid userId, CancellationToken ct = default) =>
        db.UserContacts.FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task AddAsync(UserContact contact, CancellationToken ct = default) =>
        await db.UserContacts.AddAsync(contact, ct);

    public void Update(UserContact contact) => db.UserContacts.Update(contact);
}

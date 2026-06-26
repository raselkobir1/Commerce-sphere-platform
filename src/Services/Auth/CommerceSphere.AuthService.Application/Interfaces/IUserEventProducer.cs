using CommerceSphere.Shared.Contracts.Events.Auth;

namespace CommerceSphere.AuthService.Application.Interfaces;

public interface IUserEventProducer
{
    Task PublishUserCreatedAsync(UserCreatedEvent evt, CancellationToken ct = default);
}

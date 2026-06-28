namespace CommerceSphere.NotificationService.Domain.Entities;

// A local read-model of "who to email", built by consuming user-created events. Lets the
// notification service email customers without calling the Auth service at send time.
public class UserContact
{
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;

    private UserContact() { }

    public static UserContact Create(Guid userId, string email, string firstName) =>
        new() { UserId = userId, Email = email, FirstName = firstName };

    public void Update(string email, string firstName)
    {
        Email = email;
        FirstName = firstName;
    }
}

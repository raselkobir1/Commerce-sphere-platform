namespace CommerceSphere.ChatService.Domain.Entities;

// Who sent a chat message. Stored as a short string so the value is self-describing in the DB.
public static class SenderRole
{
    public const string Customer = "Customer";
    public const string Support = "Support";
}

namespace CommerceSphere.CartService.Application.DTOs.Responses;

public record NotificationResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    int ItemCount,
    bool IsRead,
    DateTime CreatedAt);

// The payload the admin panel loads on startup (and refreshes after marking read).
public record NotificationListResponse(
    IReadOnlyList<NotificationResponse> Items,
    int UnreadCount);

namespace CommerceSphere.NotificationService.Application.DTOs.Responses;

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

public record NotificationListResponse(
    IReadOnlyList<NotificationResponse> Items,
    int UnreadCount);

namespace CommerceSphere.ChatService.Application.DTOs.Requests;

// The authenticated caller's identity, extracted from JWT claims by the API layer and passed to
// the manager so the Application layer never touches HttpContext.
public record ChatUser(
    Guid UserId,
    string Name,
    string Email,
    bool IsSupport   // true for support agents (Admin role), false for customers
);

// Body of a send-message request.
public record SendMessageRequest(
    string Content
);

using CommerceSphere.NotificationService.Application.Interfaces;
using CommerceSphere.NotificationService.Application.Managers;
using CommerceSphere.NotificationService.Domain.Entities;
using CommerceSphere.NotificationService.Domain.Interfaces;
using CommerceSphere.Shared.Contracts.Events.Auth;
using CommerceSphere.Shared.Contracts.Events.Cart;
using Microsoft.Extensions.Logging;

namespace CommerceSphere.NotificationService.Application.Handlers;

// The heart of the service. Each handler is idempotent via the inbox table, so a redelivered
// Kafka message is recognised and skipped (no double notify). Side effects (email, realtime
// push) run BEFORE the inbox row is committed, so a transient failure is retried — an email is
// never silently dropped. The in-app notification + inbox row commit together, so the in-app
// record is created exactly once even across a crash.
public class OrderEventHandler(
    IUnitOfWork uow,
    IEmailSender email,
    IRealtimeNotifier realtime,
    ILogger<OrderEventHandler> logger) : IOrderEventHandler
{
    public async Task HandleCheckedOutAsync(CartCheckedOutEvent evt, CancellationToken ct = default)
    {
        var key = $"checkedout:{evt.CartId}";
        if (await uow.Inbox.ExistsAsync(key, ct))
        {
            logger.LogInformation("Order {OrderId} already notified. Skipping.", evt.CartId);
            return;
        }

        var itemCount = evt.Items.Count;
        var orderRef = "#" + evt.CartId.ToString("N")[..8].ToUpperInvariant();

        // 1) Side effects first (retried on failure, so nothing is missed).
        var contact = await uow.Contacts.GetByIdAsync(evt.UserId, ct);
        if (contact is not null)
            await email.SendOrderConfirmationAsync(contact.Email, contact.FirstName, orderRef, evt.TotalAmount, itemCount, ct);
        else
            logger.LogWarning("No contact for user {UserId}; skipping confirmation email for {OrderId}.", evt.UserId, evt.CartId);

        var notification = Notification.OrderPlaced(evt.CartId, evt.UserId, evt.TotalAmount, itemCount);
        await realtime.NotificationCreatedAsync(NotificationManager.Map(notification), ct);

        // 2) Persist the in-app notification + the inbox guard together (exactly-once in-app record).
        await uow.Notifications.AddAsync(notification, ct);
        await uow.Inbox.AddAsync(InboxMessage.For(key), ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Order-placed notification handled for {OrderId}.", evt.CartId);
    }

    public async Task HandleCancelledAsync(CartCancelledEvent evt, CancellationToken ct = default)
    {
        var key = $"cancelled:{evt.CartId}";
        if (await uow.Inbox.ExistsAsync(key, ct))
        {
            logger.LogInformation("Cancellation for {OrderId} already notified. Skipping.", evt.CartId);
            return;
        }

        // The customer cancellation email is owned by the Auth service; here we only raise the
        // admin in-app notification so the dashboard reflects the cancellation live.
        var notification = Notification.OrderCancelled(evt.CartId, evt.UserId, evt.TotalAmount, evt.Items.Count);
        await realtime.NotificationCreatedAsync(NotificationManager.Map(notification), ct);

        await uow.Notifications.AddAsync(notification, ct);
        await uow.Inbox.AddAsync(InboxMessage.For(key), ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Order-cancelled notification handled for {OrderId}.", evt.CartId);
    }

    public async Task HandleUserCreatedAsync(UserCreatedEvent evt, CancellationToken ct = default)
    {
        // Upsert is naturally idempotent — no inbox row needed.
        var existing = await uow.Contacts.GetByIdAsync(evt.UserId, ct);
        if (existing is null)
        {
            await uow.Contacts.AddAsync(UserContact.Create(evt.UserId, evt.Email, evt.FirstName), ct);
        }
        else
        {
            existing.Update(evt.Email, evt.FirstName);
            uow.Contacts.Update(existing);
        }
        await uow.SaveChangesAsync(ct);
    }
}

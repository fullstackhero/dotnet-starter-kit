using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Chat.Contracts.Events;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>
/// Subscribes to <see cref="MentionedInChannelIntegrationEvent"/> emitted by the Chat module's
/// <c>SendMessageCommandHandler</c>. Writes a row to the caller's inbox and pushes a
/// <c>NotificationCreated</c> event to the mentioned user's SignalR group so the bell badge
/// updates live.
///
/// The handler runs in the same scope as the Chat publish (in-memory bus, synchronous dispatch),
/// so any exception will surface to the request — we keep the work minimal to avoid making
/// SendMessage slow.
/// </summary>
public sealed class MentionedInChannelIntegrationEventHandler(
    NotificationsDbContext db,
    IHubContext<AppHub> hub,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    ILogger<MentionedInChannelIntegrationEventHandler> logger)
    : IIntegrationEventHandler<MentionedInChannelIntegrationEvent>
{
    public async Task HandleAsync(MentionedInChannelIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Fail loud on a tenant mismatch rather than write to the wrong tenant: the DbContext captures its
        // tenant at construction, so a future publisher omitting the context would leak cross-tenant silently.
        var ambientTenantId = tenantAccessor.MultiTenantContext.TenantInfo?.Id;
        if (!string.Equals(ambientTenantId, @event.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Tenant context mismatch handling {nameof(MentionedInChannelIntegrationEvent)}: ambient " +
                $"'{ambientTenantId ?? "(none)"}' != event '{@event.TenantId ?? "(none)"}'. The publisher must " +
                "establish the tenant's Finbuckle context before publishing (see eventing rules).");
        }

        var notification = Notification.Create(
            userId: @event.MentionedUserId,
            type: "chat.mention",
            title: string.IsNullOrEmpty(@event.ChannelName)
                ? "You were mentioned in a conversation"
                : $"You were mentioned in #{@event.ChannelName}",
            body: @event.BodyPreview,
            link: $"/chat/{@event.ChannelId}?messageId={@event.MessageId}",
            source: @event.Source,
            metadata: new
            {
                channelId = @event.ChannelId,
                channelName = @event.ChannelName,
                messageId = @event.MessageId,
                authorUserId = @event.AuthorUserId,
            });

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await hub.Clients.Group($"user:{@event.MentionedUserId}")
            .SendAsync("NotificationCreated", new
            {
                id = notification.Id,
                type = notification.Type,
                title = notification.Title,
                body = notification.Body,
                link = notification.Link,
                source = notification.Source,
                createdAtUtc = notification.CreatedAtUtc,
            }, ct)
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "Mention notification {NotificationId} for user {UserId} from {AuthorUserId} in channel {ChannelId}",
                notification.Id, @event.MentionedUserId, @event.AuthorUserId, @event.ChannelId);
        }
    }
}

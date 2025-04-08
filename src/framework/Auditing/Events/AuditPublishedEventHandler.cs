using FSH.Framework.Auditing.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Auditing.Events;

public class AuditPublishedEventHandler(
    ILogger<AuditPublishedEventHandler> logger,
    IAuditTrailDbContext context)
    : INotificationHandler<AuditPublishedEvent>
{
    public async Task Handle(AuditPublishedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Trails == null || notification.Trails.Count == 0)
        {
            logger.LogDebug("No audit trails to persist.");
            return;
        }

        try
        {
            await context.AuditTrails.AddRangeAsync(notification.Trails, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Persisted {Count} audit trail(s).", notification.Trails.Count);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database update error while saving audit trails.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while saving audit trails.");
        }
    }
}

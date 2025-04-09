using FSH.Framework.Auditing.Contracts.Events;
using FSH.Framework.Auditing.Core.Abstractions;
using FSH.Framework.Core.Messaging.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace FSH.Framework.Auditing.Infrastructure.Handlers;

[SuppressMessage("Performance", "CA1848")]
[SuppressMessage("Design", "CA1031")]
public class AuditPublishedEventHandler(
    ILogger<AuditPublishedEventHandler> logger,
    IAuditingDbContext context)
    : IEventHandler<AuditPublishedEvent>
{
    public async Task HandleAsync(AuditPublishedEvent notification, CancellationToken cancellationToken = default)
    {
        if (notification.Trails == null || notification.Trails.Count == 0)
        {
            logger.LogDebug("No audit trails to persist.");
            return;
        }

        try
        {
            await context.Trails.AddRangeAsync(notification.Trails, cancellationToken);
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

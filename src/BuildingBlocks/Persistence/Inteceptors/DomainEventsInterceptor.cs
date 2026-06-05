using FSH.Framework.Core.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Persistence.Inteceptors;

/// <summary>
/// Entity Framework interceptor that automatically publishes domain events after saving changes.
/// </summary>
public sealed class DomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventsInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventsInterceptor"/> class.
    /// </summary>
    /// <param name="publisher">The mediator publisher for publishing domain events.</param>
    /// <param name="logger">Logger for tracking domain event publication.</param>
    public DomainEventsInterceptor(IPublisher publisher, ILogger<DomainEventsInterceptor> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Called after changes have been saved to the database. Publishes all domain events from tracked entities.
    /// </summary>
    /// <param name="eventData">Contextual information about the completed save operation.</param>
    /// <param name="result">The number of state entries written to the database.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <exception cref="ArgumentNullException">Thrown when eventData is null.</exception>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context == null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);

        var domainEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .SelectMany(e =>
            {
                var pending = e.Entity.DomainEvents.ToArray();
                e.Entity.ClearDomainEvents();
                return pending;
            })
            .ToArray();

        if (domainEvents.Length == 0)
            return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Publishing {Count} domain events...", domainEvents.Length);
        }

        foreach (var domainEvent in domainEvents)
        {
            try
            {
                await _publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Handler failures must not fail the already-committed save (events collected post-
                // SaveChanges). Handlers needing guaranteed delivery should use the outbox pattern.
                _logger.LogError(ex, "Failed to publish domain event {EventType}", domainEvent.GetType().Name);
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }
}
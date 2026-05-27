namespace FSH.Framework.Eventing.Inbox;

/// <summary>
/// Abstraction for idempotent consumer tracking.
/// </summary>
public interface IInboxStore
{
    Task<bool> HasProcessedAsync(Guid eventId, string handlerName, CancellationToken ct = default);

    Task MarkProcessedAsync(Guid eventId, string handlerName, string? tenantId, string eventType, CancellationToken ct = default);
}
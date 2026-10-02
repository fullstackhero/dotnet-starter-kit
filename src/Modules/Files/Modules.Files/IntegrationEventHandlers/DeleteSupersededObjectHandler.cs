using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Files.Contracts.Events;
using FSH.Modules.Files.Services;

namespace FSH.Modules.Files.IntegrationEventHandlers;

/// <summary>
/// The durable half of a storage move. <see cref="FileStorageRelocator"/> tries to delete the old object
/// right after committing the move, but that attempt can fail silently (the store swallows errors) or be
/// lost to a crash, and on a Public → Private flip the leftover would stay anonymously readable under
/// <c>public/</c>. This event commits in the same transaction as the move, so the outbox always delivers
/// it; the handler deletes the old object if nothing references it and throws while it is still there,
/// which makes the outbox retry with backoff.
/// </summary>
public sealed class DeleteSupersededObjectHandler(FileStorageRelocator relocator)
    : IIntegrationEventHandler<FileStorageKeyChangedIntegrationEvent>
{
    public Task HandleAsync(FileStorageKeyChangedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return relocator.DeleteIfUnreferencedAsync(@event.FileAssetId, @event.OldStorageKey, ct);
    }
}

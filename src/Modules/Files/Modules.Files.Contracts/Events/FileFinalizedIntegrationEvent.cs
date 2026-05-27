using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Files.Contracts.Events;

/// <summary>
/// Raised when a FileAsset transitions from PendingUpload to Available (or Quarantined). Owning
/// modules can subscribe to react to the upload completing — e.g. update a search index, send a
/// notification, etc.
/// </summary>
public sealed record FileFinalizedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid FileAssetId,
    string OwnerType,
    Guid? OwnerId,
    string ContentType,
    long SizeBytes,
    int FinalStatus) : IIntegrationEvent;

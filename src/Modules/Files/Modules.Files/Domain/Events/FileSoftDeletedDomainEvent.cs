using FSH.Framework.Core.Domain;

namespace FSH.Modules.Files.Domain.Events;

public sealed record FileSoftDeletedDomainEvent(
    Guid FileAssetId,
    string ActorUserId,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);

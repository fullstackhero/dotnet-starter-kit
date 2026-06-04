using FSH.Framework.Core.Domain;
using FSH.Modules.Files.Contracts.v1.DTOs;

namespace FSH.Modules.Files.Domain.Events;

public sealed record FileFinalizedDomainEvent(
    Guid FileAssetId,
    string OwnerType,
    Guid? OwnerId,
    FileAssetStatus FinalStatus,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);

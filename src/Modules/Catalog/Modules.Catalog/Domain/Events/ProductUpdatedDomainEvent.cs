using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain.Events;

public sealed record ProductUpdatedDomainEvent(
    Guid ProductId,
    string Name,
    Guid Id,
    DateTimeOffset OccurredOnUtc)
    : DomainEvent(Id, OccurredOnUtc);

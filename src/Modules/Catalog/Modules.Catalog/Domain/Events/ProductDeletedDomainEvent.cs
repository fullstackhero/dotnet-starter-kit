using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain.Events;

public sealed record ProductDeletedDomainEvent(
    Guid ProductId,
    Guid Id,
    DateTimeOffset OccurredOnUtc)
    : DomainEvent(Id, OccurredOnUtc);

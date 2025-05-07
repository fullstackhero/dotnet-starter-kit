using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain.Events;

public sealed record AgencyCreated : DomainEvent
{
    public Agency Agency { get; set; } = default!;
}


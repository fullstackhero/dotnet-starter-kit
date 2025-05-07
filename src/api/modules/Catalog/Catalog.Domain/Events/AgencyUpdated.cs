using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain.Events;

public sealed record AgencyUpdated : DomainEvent
{
    public Agency Agency { get; set; } = default!;
}

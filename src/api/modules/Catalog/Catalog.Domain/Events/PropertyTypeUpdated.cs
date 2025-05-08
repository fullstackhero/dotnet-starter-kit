using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain.Events;

public sealed record PropertyTypeUpdated : DomainEvent
{
    public PropertyType? PropertyType { get; set; }
}

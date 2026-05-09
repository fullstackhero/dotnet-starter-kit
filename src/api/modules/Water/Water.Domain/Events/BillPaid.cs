using FSH.Framework.Core.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain.Events;

public sealed record BillPaid : DomainEvent
{
    public Bill? Bill { get; set; }
}

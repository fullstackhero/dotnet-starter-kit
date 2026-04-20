using FSH.Framework.Core.Domain;
using FSH.Framework.Shared.Quota;
using FSH.Modules.Billing.Contracts;

namespace FSH.Modules.Billing.Domain;

/// <summary>
/// A single line on an invoice. Amount is computed as Quantity * UnitPrice and is stored so that
/// summing <see cref="Invoice.SubtotalAmount"/> doesn't require re-traversing the lines each time.
/// </summary>
public sealed class InvoiceLineItem : BaseEntity<Guid>
{
    public Guid InvoiceId { get; private set; }
    public InvoiceLineItemKind Kind { get; private set; }
    public QuotaResource? Resource { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Amount { get; private set; }

    private InvoiceLineItem() { }

    internal static InvoiceLineItem Create(
        Guid invoiceId,
        InvoiceLineItemKind kind,
        string description,
        decimal quantity,
        decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        }
        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice cannot be negative.");
        }

        return new InvoiceLineItem
        {
            Id = Guid.CreateVersion7(),
            InvoiceId = invoiceId,
            Kind = kind,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Amount = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero)
        };
    }

    internal void AttachResource(QuotaResource resource) => Resource = resource;
}

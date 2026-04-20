using FSH.Framework.Core.Domain;
using FSH.Modules.Billing.Contracts;

namespace FSH.Modules.Billing.Domain;

/// <summary>
/// An invoice for a tenant covering a single monthly period. Starts as Draft, transitions to
/// Issued when sent to the customer, then to Paid or Void. Totals are recomputed every time a
/// line is added so callers don't have to.
/// </summary>
public sealed class Invoice : AggregateRoot<Guid>
{
    private readonly List<InvoiceLineItem> _lineItems = new();

    public string TenantId { get; private set; } = default!;
    public string InvoiceNumber { get; private set; } = default!;
    public int PeriodYear { get; private set; }
    public int PeriodMonth { get; private set; }
    public string Currency { get; private set; } = "USD";
    public decimal SubtotalAmount { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? IssuedAtUtc { get; private set; }
    public DateTime? DueAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? VoidedAtUtc { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems;

    private Invoice() { }

    public static Invoice CreateDraft(
        string tenantId,
        string invoiceNumber,
        int periodYear,
        int periodMonth,
        string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (periodYear is < 2000 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(periodYear));
        }
        if (periodMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(periodMonth));
        }

        return new Invoice
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            InvoiceNumber = invoiceNumber,
            PeriodYear = periodYear,
            PeriodMonth = periodMonth,
            Currency = currency.ToUpperInvariant(),
            Status = InvoiceStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public InvoiceLineItem AddLineItem(InvoiceLineItemKind kind, string description, decimal quantity, decimal unitPrice)
    {
        RequireStatus(InvoiceStatus.Draft);
        var line = InvoiceLineItem.Create(Id, kind, description, quantity, unitPrice);
        _lineItems.Add(line);
        RecalculateTotals();
        return line;
    }

    public void Issue(DateTime? dueAtUtc = null)
    {
        RequireStatus(InvoiceStatus.Draft);
        Status = InvoiceStatus.Issued;
        IssuedAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc is null
            ? IssuedAtUtc.Value.AddDays(14)
            : DateTime.SpecifyKind(dueAtUtc.Value, DateTimeKind.Utc);
    }

    public void MarkPaid()
    {
        if (Status is InvoiceStatus.Paid)
        {
            return;
        }
        if (Status is not InvoiceStatus.Issued)
        {
            throw new InvalidOperationException($"Cannot mark invoice as paid from status {Status}.");
        }
        Status = InvoiceStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;
    }

    public void Void(string? reason = null)
    {
        if (Status is InvoiceStatus.Paid)
        {
            throw new InvalidOperationException("Paid invoices cannot be voided.");
        }
        Status = InvoiceStatus.Void;
        VoidedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? reason : $"{Notes}; Voided: {reason}";
        }
    }

    public void SetNotes(string? notes)
    {
        Notes = notes;
    }

    private void RequireStatus(InvoiceStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Operation requires invoice status {expected} but was {Status}.");
        }
    }

    private void RecalculateTotals()
    {
        SubtotalAmount = _lineItems.Sum(l => l.Amount);
    }
}

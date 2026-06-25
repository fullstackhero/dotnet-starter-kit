using FSH.Framework.Core.Domain;
using FSH.Modules.Billing.Contracts;

namespace FSH.Modules.Billing.Domain;

public sealed class TopupRequest : AggregateRoot<Guid>
{
    public string TenantId { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string? Note { get; private set; }
    public TopupRequestStatus Status { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public string? RequestedBy { get; private set; }
    public string? DecisionNote { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DecidedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private TopupRequest() { }

    public static TopupRequest Create(string tenantId, decimal amount, string currency, string? note, string? requestedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);
        return new TopupRequest
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency,
            Note = note,
            RequestedBy = requestedBy,
            Status = TopupRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkInvoiced(Guid invoiceId, string? note)
    {
        Require(TopupRequestStatus.Pending);
        InvoiceId = invoiceId;
        DecisionNote = note;
        Status = TopupRequestStatus.Invoiced;
        DecidedAtUtc = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Require(TopupRequestStatus.Invoiced);
        Status = TopupRequestStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Reject(string? reason)
    {
        Require(TopupRequestStatus.Pending);
        DecisionNote = reason;
        Status = TopupRequestStatus.Rejected;
        DecidedAtUtc = DateTime.UtcNow;
    }

    private void Require(TopupRequestStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Top-up request must be {expected} (was {Status}).");
    }
}

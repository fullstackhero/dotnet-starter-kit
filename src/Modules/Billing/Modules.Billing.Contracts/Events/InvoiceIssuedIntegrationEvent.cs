using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Billing.Contracts.Events;

/// <summary>
/// Raised when an invoice transitions to Issued and becomes a real bill (e.g. the subscription invoice
/// generated on tenant create/renew). Consumers notify the tenant that an invoice is due.
/// </summary>
public sealed record InvoiceIssuedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    DateTime? DueAtUtc,
    int PeriodYear,
    int PeriodMonth)
    : IIntegrationEvent;

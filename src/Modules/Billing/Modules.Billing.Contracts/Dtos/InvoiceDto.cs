namespace FSH.Modules.Billing.Contracts.Dtos;

public sealed record InvoiceDto(
    Guid Id,
    string TenantId,
    string InvoiceNumber,
    int PeriodYear,
    int PeriodMonth,
    string Currency,
    decimal SubtotalAmount,
    InvoiceStatus Status,
    DateTime CreatedAtUtc,
    DateTime? IssuedAtUtc,
    DateTime? DueAtUtc,
    DateTime? PaidAtUtc,
    DateTime? VoidedAtUtc,
    string? Notes,
    IReadOnlyList<InvoiceLineItemDto> LineItems,
    InvoicePurpose Purpose,
    DateTime? PeriodStartUtc,
    DateTime? PeriodEndUtc);

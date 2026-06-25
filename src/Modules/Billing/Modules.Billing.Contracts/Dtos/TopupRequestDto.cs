namespace FSH.Modules.Billing.Contracts.Dtos;

public sealed record TopupRequestDto(
    Guid Id,
    string TenantId,
    decimal Amount,
    string Currency,
    string? Note,
    string Status,
    Guid? InvoiceId,
    string? RequestedBy,
    string? DecisionNote,
    DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc,
    DateTime? CompletedAtUtc);

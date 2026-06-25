namespace FSH.Modules.Billing.Contracts.Dtos;

public sealed record WalletTransactionDto(
    Guid Id,
    decimal Amount,
    string Kind,
    string Description,
    string? ReferenceId,
    DateTime CreatedAtUtc);

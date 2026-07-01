using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Domain;

namespace FSH.Modules.Billing.Mappings;

internal static class WalletMappings
{
    public static WalletTransactionDto ToDto(this WalletTransaction t)
        => new(t.Id, t.Amount.Amount, t.Kind.ToString(), t.Description, t.ReferenceId, t.CreatedAtUtc);

    public static WalletDto ToDto(this Wallet w, int recentCount = 10)
        => new(
            w.Id, w.TenantId, w.Currency, w.Balance.Amount, w.Status.ToString(), w.CreatedAtUtc,
            w.Transactions
                .OrderByDescending(t => t.CreatedAtUtc)
                .Take(recentCount)
                .Select(t => t.ToDto())
                .ToList());

    public static TopupRequestDto ToDto(this TopupRequest r)
        => new(
            r.Id, r.TenantId, r.Amount.Amount, r.Amount.Currency, r.Note, r.Status.ToString(),
            r.InvoiceId, r.RequestedBy, r.DecisionNote, r.CreatedAtUtc, r.DecidedAtUtc, r.CompletedAtUtc);
}

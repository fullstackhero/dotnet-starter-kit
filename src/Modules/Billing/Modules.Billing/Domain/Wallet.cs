using FSH.Framework.Core.Domain;
using FSH.Modules.Billing.Contracts;

namespace FSH.Modules.Billing.Domain;

public sealed class Wallet : AggregateRoot<Guid>
{
    private readonly List<WalletTransaction> _transactions = new();

    public string TenantId { get; private set; } = default!;
    public Money Balance { get; private set; } = default!;
    public string Currency => Balance.Currency;
    public WalletStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyList<WalletTransaction> Transactions => _transactions;

    private Wallet() { }

    public static Wallet Create(string tenantId, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new Wallet
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Balance = Money.Zero(string.IsNullOrWhiteSpace(currency) ? "USD" : currency),
            Status = WalletStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public WalletTransaction Credit(decimal amount, WalletTransactionKind kind, string description, string? referenceId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);
        var credit = new Money(amount, Balance.Currency);
        var tx = WalletTransaction.Create(Id, TenantId, credit, kind, description, referenceId);
        _transactions.Add(tx);
        Balance = Balance.Add(credit);
        UpdatedAtUtc = DateTime.UtcNow;
        return tx;
    }

    public WalletTransaction Debit(decimal amount, WalletTransactionKind kind, string description, string? referenceId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);
        if (amount > Balance.Amount)
            throw new InvalidOperationException("Insufficient wallet balance.");
        var debit = new Money(amount, Balance.Currency);
        var tx = WalletTransaction.Create(Id, TenantId, new Money(-amount, Balance.Currency), kind, description, referenceId);
        _transactions.Add(tx);
        Balance = Balance.Subtract(debit);
        UpdatedAtUtc = DateTime.UtcNow;
        return tx;
    }
}

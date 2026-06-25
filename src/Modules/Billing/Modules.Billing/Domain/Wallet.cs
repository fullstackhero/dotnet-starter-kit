using FSH.Framework.Core.Domain;
using FSH.Modules.Billing.Contracts;

namespace FSH.Modules.Billing.Domain;

public sealed class Wallet : AggregateRoot<Guid>
{
    private readonly List<WalletTransaction> _transactions = new();

    public string TenantId { get; private set; } = default!;
    public string Currency { get; private set; } = "USD";
    public decimal Balance { get; private set; }
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
            Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency,
            Balance = 0m,
            Status = WalletStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public WalletTransaction Credit(decimal amount, WalletTransactionKind kind, string description, string? referenceId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);
        var tx = WalletTransaction.Create(Id, TenantId, amount, kind, description, referenceId);
        _transactions.Add(tx);
        Balance += amount;
        UpdatedAtUtc = DateTime.UtcNow;
        return tx;
    }

    public WalletTransaction Debit(decimal amount, WalletTransactionKind kind, string description, string? referenceId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient wallet balance.");
        var tx = WalletTransaction.Create(Id, TenantId, -amount, kind, description, referenceId);
        _transactions.Add(tx);
        Balance -= amount;
        UpdatedAtUtc = DateTime.UtcNow;
        return tx;
    }
}

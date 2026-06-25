using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;
using Shouldly;
using Xunit;

namespace Billing.Tests.Domain;

public sealed class WalletTests
{
    [Fact]
    public void Create_starts_active_with_zero_balance()
    {
        var w = Wallet.Create("tenant-a", "USD");
        w.TenantId.ShouldBe("tenant-a");
        w.Currency.ShouldBe("USD");
        w.Balance.ShouldBe(0m);
        w.Status.ShouldBe(WalletStatus.Active);
        w.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Credit_increases_balance_and_returns_ledger_row()
    {
        var w = Wallet.Create("tenant-a", "USD");
        var tx = w.Credit(50m, WalletTransactionKind.Topup, "Top-up", "req-1");
        w.Balance.ShouldBe(50m);
        tx.Amount.ShouldBe(50m);
        tx.WalletId.ShouldBe(w.Id);
        tx.TenantId.ShouldBe("tenant-a");
        tx.ReferenceId.ShouldBe("req-1");
    }

    [Fact]
    public void Credit_rejects_non_positive_amount()
        => Should.Throw<ArgumentOutOfRangeException>(
            () => Wallet.Create("t", "USD").Credit(0m, WalletTransactionKind.Topup, "x", null));

    [Fact]
    public void Debit_beyond_balance_throws()
    {
        var w = Wallet.Create("tenant-a", "USD");
        w.Credit(10m, WalletTransactionKind.Topup, "Top-up", null);
        Should.Throw<InvalidOperationException>(
            () => w.Debit(25m, WalletTransactionKind.MessageCharge, "msg", null));
    }
}

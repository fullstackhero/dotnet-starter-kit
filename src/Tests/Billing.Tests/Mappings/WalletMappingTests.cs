using FSH.Modules.Billing.Domain;
using FSH.Modules.Billing.Mappings;
using FSH.Modules.Billing.Contracts;
using Shouldly;
using Xunit;

namespace Billing.Tests.Mappings;

public sealed class WalletMappingTests
{
    [Fact]
    public void Wallet_ToDto_emits_string_status_and_balance()
    {
        var w = Wallet.Create("tenant-a", "USD");
        w.Credit(50m, WalletTransactionKind.Topup, "Top-up", "req-1");
        var dto = w.ToDto();
        dto.Balance.ShouldBe(50m);
        dto.Status.ShouldBe("Active");
        dto.RecentTransactions.Count.ShouldBe(1);
        dto.RecentTransactions[0].Kind.ShouldBe("Topup");
    }

    [Fact]
    public void TopupRequest_ToDto_emits_string_status()
    {
        var r = TopupRequest.Create("tenant-a", 25m, "USD", "note", "u1");
        r.ToDto().Status.ShouldBe("Pending");
    }
}

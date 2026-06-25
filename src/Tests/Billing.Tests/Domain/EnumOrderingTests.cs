using FSH.Modules.Billing.Contracts;
using Shouldly;
using Xunit;

namespace Billing.Tests.Domain;

public sealed class EnumOrderingTests
{
    [Fact]
    public void InvoicePurpose_Topup_is_two()
        => ((int)InvoicePurpose.Topup).ShouldBe(2);

    [Fact]
    public void TopupRequestStatus_Pending_is_default_zero()
        => ((int)TopupRequestStatus.Pending).ShouldBe(0);

    [Fact]
    public void WalletStatus_Active_is_default_zero()
        => ((int)WalletStatus.Active).ShouldBe(0);

    [Fact]
    public void WalletTransactionKind_Topup_is_default_zero()
        => ((int)WalletTransactionKind.Topup).ShouldBe(0);
}

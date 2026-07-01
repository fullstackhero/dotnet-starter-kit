using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;

namespace Billing.Tests.Domain;

/// <summary>
/// InvoiceLineItem.Create is internal; line items are exercised through
/// <see cref="Invoice.AddLineItem"/> which is the only public construction path.
/// </summary>
public sealed class InvoiceLineItemTests
{
    private static Invoice NewDraft() =>
        Invoice.CreateDraft("tenant-1", "INV-202601-tenant-1", 2026, 1, "USD");

    #region Happy Path

    [Fact]
    public void Create_Should_Compute_Amount_As_Quantity_Times_UnitPrice()
    {
        var inv = NewDraft();

        var line = inv.AddLineItem(InvoiceLineItemKind.Overage, "calls", 100m, 0.02m);

        line.Quantity.ShouldBe(100m);
        line.UnitPrice.ShouldBe(0.02m);
        line.Amount.Amount.ShouldBe(2.00m);
        line.InvoiceId.ShouldBe(inv.Id);
        line.Kind.ShouldBe(InvoiceLineItemKind.Overage);
    }

    [Fact]
    public void Create_Should_Allow_Zero_Quantity_And_Zero_Price()
    {
        var inv = NewDraft();

        var line = inv.AddLineItem(InvoiceLineItemKind.Adjustment, "credit", 0m, 0m);

        line.Amount.Amount.ShouldBe(0m);
    }

    #endregion

    #region Exceptions

    [Fact]
    public void Create_Should_Throw_When_Description_Blank()
    {
        var inv = NewDraft();

        Should.Throw<ArgumentException>(() =>
            inv.AddLineItem(InvoiceLineItemKind.BaseFee, "  ", 1m, 1m));
    }

    [Fact]
    public void Create_Should_Throw_When_Quantity_Negative()
    {
        var inv = NewDraft();

        Should.Throw<ArgumentOutOfRangeException>(() =>
            inv.AddLineItem(InvoiceLineItemKind.BaseFee, "bad qty", -1m, 1m));
    }

    [Fact]
    public void Create_Should_Throw_When_UnitPrice_Negative()
    {
        var inv = NewDraft();

        Should.Throw<ArgumentOutOfRangeException>(() =>
            inv.AddLineItem(InvoiceLineItemKind.BaseFee, "bad price", 1m, -1m));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Resource_Should_Be_Null_Until_Attached()
    {
        var inv = NewDraft();

        var line = inv.AddLineItem(InvoiceLineItemKind.Overage, "no-resource", 1m, 1m);

        line.Resource.ShouldBeNull();
    }

    #endregion
}

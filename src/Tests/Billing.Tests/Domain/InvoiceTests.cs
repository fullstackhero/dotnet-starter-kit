using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;

namespace Billing.Tests.Domain;

public sealed class InvoiceTests
{
    private static Invoice NewDraft() =>
        Invoice.CreateDraft("tenant-1", "INV-202601-tenant-1", 2026, 1, "usd");

    #region Purpose / period span

    [Fact]
    public void CreateDraft_Should_Default_To_Usage_Purpose()
    {
        var inv = NewDraft();

        inv.Purpose.ShouldBe(InvoicePurpose.Usage);
        inv.PeriodStartUtc.ShouldBeNull();
        inv.PeriodEndUtc.ShouldBeNull();
    }

    [Fact]
    public void CreateDraft_Subscription_Should_Carry_Purpose_And_Period_Span()
    {
        var start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(12);

        var inv = Invoice.CreateDraft("acme", "SUB-202605-acme", 2026, 5, "USD",
            InvoicePurpose.Subscription, start, end);

        inv.Purpose.ShouldBe(InvoicePurpose.Subscription);
        inv.PeriodStartUtc.ShouldBe(start);
        inv.PeriodEndUtc.ShouldBe(end);
    }

    [Fact]
    public void CreateDraft_Subscription_Should_Normalize_Period_To_Utc()
    {
        var start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var end = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var inv = Invoice.CreateDraft("acme", "SUB-202605-acme", 2026, 5, "USD",
            InvoicePurpose.Subscription, start, end);

        inv.PeriodStartUtc!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        inv.PeriodEndUtc!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    #endregion

    #region Happy Path

    [Fact]
    public void CreateDraft_Should_Start_Draft_With_Upper_Currency()
    {
        var inv = NewDraft();

        inv.Status.ShouldBe(InvoiceStatus.Draft);
        inv.Currency.ShouldBe("USD");
        inv.SubtotalAmount.ShouldBe(0m);
        inv.LineItems.ShouldBeEmpty();
    }

    [Fact]
    public void AddLineItem_Should_Append_And_Recalculate_Subtotal()
    {
        var inv = NewDraft();

        inv.AddLineItem(InvoiceLineItemKind.BaseFee, "Base", 1m, 49m);
        inv.AddLineItem(InvoiceLineItemKind.Overage, "Overage", 2m, 10m);

        inv.LineItems.Count.ShouldBe(2);
        inv.SubtotalAmount.ShouldBe(69m);
    }

    [Fact]
    public void Issue_Should_Default_Due_Date_To_14_Days_After_Issue()
    {
        var inv = NewDraft();

        inv.Issue();

        inv.Status.ShouldBe(InvoiceStatus.Issued);
        inv.IssuedAtUtc.ShouldNotBeNull();
        inv.DueAtUtc.ShouldNotBeNull();
        (inv.DueAtUtc!.Value - inv.IssuedAtUtc!.Value).Days.ShouldBe(14);
    }

    [Fact]
    public void Issue_Should_Use_Provided_Due_Date_Normalized_To_Utc()
    {
        var inv = NewDraft();
        var due = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);

        inv.Issue(due);

        inv.DueAtUtc!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        inv.DueAtUtc.Value.Date.ShouldBe(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc).Date);
    }

    [Fact]
    public void MarkPaid_Should_Transition_Issued_To_Paid()
    {
        var inv = NewDraft();
        inv.Issue();

        inv.MarkPaid();

        inv.Status.ShouldBe(InvoiceStatus.Paid);
        inv.PaidAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void MarkPaid_Should_Be_Idempotent_When_Already_Paid()
    {
        var inv = NewDraft();
        inv.Issue();
        inv.MarkPaid();
        var firstPaidAt = inv.PaidAtUtc;

        inv.MarkPaid();

        inv.Status.ShouldBe(InvoiceStatus.Paid);
        inv.PaidAtUtc.ShouldBe(firstPaidAt);
    }

    [Fact]
    public void Void_Should_Transition_Draft_To_Void_And_Append_Reason_Note()
    {
        var inv = NewDraft();

        inv.Void("duplicate");

        inv.Status.ShouldBe(InvoiceStatus.Void);
        inv.VoidedAtUtc.ShouldNotBeNull();
        inv.Notes.ShouldBe("duplicate");
    }

    [Fact]
    public void Void_Should_Combine_Reason_With_Existing_Notes()
    {
        var inv = NewDraft();
        inv.SetNotes("original note");

        inv.Void("mistake");

        inv.Notes.ShouldBe("original note; Voided: mistake");
    }

    #endregion

    #region Exceptions

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void CreateDraft_Should_Throw_When_Year_Out_Of_Range(int year)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Invoice.CreateDraft("tenant-1", "INV", year, 1, "USD"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void CreateDraft_Should_Throw_When_Month_Out_Of_Range(int month)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Invoice.CreateDraft("tenant-1", "INV", 2026, month, "USD"));
    }

    [Fact]
    public void AddLineItem_Should_Throw_When_Not_Draft()
    {
        var inv = NewDraft();
        inv.Issue();

        Should.Throw<InvalidOperationException>(() =>
            inv.AddLineItem(InvoiceLineItemKind.Adjustment, "late", 1m, 5m));
    }

    [Fact]
    public void Issue_Should_Throw_When_Not_Draft()
    {
        var inv = NewDraft();
        inv.Issue();

        Should.Throw<InvalidOperationException>(() => inv.Issue());
    }

    [Fact]
    public void MarkPaid_Should_Throw_When_Still_Draft()
    {
        var inv = NewDraft();

        Should.Throw<InvalidOperationException>(() => inv.MarkPaid());
    }

    [Fact]
    public void MarkPaid_Should_Throw_When_Voided()
    {
        var inv = NewDraft();
        inv.Void();

        Should.Throw<InvalidOperationException>(() => inv.MarkPaid());
    }

    [Fact]
    public void Void_Should_Throw_When_Already_Paid()
    {
        var inv = NewDraft();
        inv.Issue();
        inv.MarkPaid();

        Should.Throw<InvalidOperationException>(() => inv.Void("too late"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Void_Without_Reason_Should_Leave_Notes_Null()
    {
        var inv = NewDraft();

        inv.Void();

        inv.Notes.ShouldBeNull();
    }

    [Fact]
    public void AddLineItem_Should_Round_Amount_Half_Away_From_Zero()
    {
        var inv = NewDraft();

        // 0.005 * 1 = 0.005 -> rounds to 0.01 (away from zero), not banker's 0.00
        var line = inv.AddLineItem(InvoiceLineItemKind.Overage, "tiny", 1m, 0.005m);

        line.Amount.ShouldBe(0.01m);
        inv.SubtotalAmount.ShouldBe(0.01m);
    }

    [Fact]
    public void AddLineItem_Should_Round_Quantity_Times_UnitPrice_To_Two_Decimals()
    {
        var inv = NewDraft();

        // 3 * 0.333 = 0.999 -> 1.00
        var line = inv.AddLineItem(InvoiceLineItemKind.Overage, "units", 3m, 0.333m);

        line.Amount.ShouldBe(1.00m);
    }

    #endregion
}

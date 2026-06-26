using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;
using Shouldly;
using Xunit;

namespace Billing.Tests.Domain;

public sealed class TopupRequestTests
{
    [Fact]
    public void Create_starts_pending()
    {
        var r = TopupRequest.Create("tenant-a", 50m, "USD", "need credit", "user-1");
        r.Status.ShouldBe(TopupRequestStatus.Pending);
        r.Amount.Amount.ShouldBe(50m);
        r.Amount.Currency.ShouldBe("USD");
        r.InvoiceId.ShouldBeNull();
    }

    [Fact]
    public void MarkInvoiced_from_pending_links_invoice()
    {
        var r = TopupRequest.Create("tenant-a", 50m, "USD", null, null);
        var inv = Guid.CreateVersion7();
        r.MarkInvoiced(inv, "approved");
        r.Status.ShouldBe(TopupRequestStatus.Invoiced);
        r.InvoiceId.ShouldBe(inv);
        r.DecidedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void MarkCompleted_requires_invoiced()
    {
        var r = TopupRequest.Create("tenant-a", 50m, "USD", null, null);
        Should.Throw<InvalidOperationException>(() => r.MarkCompleted());
    }

    [Fact]
    public void Reject_from_invoiced_throws()
    {
        var r = TopupRequest.Create("tenant-a", 50m, "USD", null, null);
        r.MarkInvoiced(Guid.CreateVersion7(), null);
        Should.Throw<InvalidOperationException>(() => r.Reject("late"));
    }
}

using System.Text;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Services;

namespace Billing.Tests.Services;

public sealed class InvoicePdfRendererTests
{
    [Fact]
    public void Render_Should_Produce_NonEmpty_PdfDocument()
    {
        var now = DateTime.UtcNow;
        var dto = new InvoiceDto(
            Id: Guid.NewGuid(),
            TenantId: "acme",
            InvoiceNumber: "SUB-202605-abc123",
            PeriodYear: 2026,
            PeriodMonth: 5,
            Currency: "USD",
            SubtotalAmount: 29.00m,
            Status: InvoiceStatus.Issued,
            CreatedAtUtc: now,
            IssuedAtUtc: now,
            DueAtUtc: now.AddDays(14),
            PaidAtUtc: null,
            VoidedAtUtc: null,
            Notes: "Thank you for your business.",
            LineItems: new[]
            {
                new InvoiceLineItemDto(Guid.NewGuid(), InvoiceLineItemKind.BaseFee, null,
                    "Pro — Monthly subscription (2026-05-01 to 2026-06-01)", 1m, 29.00m, 29.00m),
            },
            Purpose: InvoicePurpose.Subscription,
            PeriodStartUtc: now,
            PeriodEndUtc: now.AddMonths(1));

        var sut = new InvoicePdfRenderer();
        var bytes = sut.Render(dto);

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
        Encoding.ASCII.GetString(bytes, 0, 4).ShouldBe("%PDF");
    }

    [Fact]
    public void Render_Should_Handle_Invoice_WithNoLineItems_AndNullDates()
    {
        var dto = new InvoiceDto(
            Id: Guid.NewGuid(),
            TenantId: "globex",
            InvoiceNumber: "USG-202605-def456",
            PeriodYear: 2026,
            PeriodMonth: 5,
            Currency: "EUR",
            SubtotalAmount: 0m,
            Status: InvoiceStatus.Draft,
            CreatedAtUtc: DateTime.UtcNow,
            IssuedAtUtc: null,
            DueAtUtc: null,
            PaidAtUtc: null,
            VoidedAtUtc: null,
            Notes: null,
            LineItems: Array.Empty<InvoiceLineItemDto>(),
            Purpose: InvoicePurpose.Usage,
            PeriodStartUtc: null,
            PeriodEndUtc: null);

        var bytes = new InvoicePdfRenderer().Render(dto);

        bytes.Length.ShouldBeGreaterThan(0);
        Encoding.ASCII.GetString(bytes, 0, 4).ShouldBe("%PDF");
    }
}

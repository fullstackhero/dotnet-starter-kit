using System.Globalization;
using FSH.Modules.Billing.Contracts.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FSH.Modules.Billing.Services;

/// <summary>
/// QuestPDF-based invoice renderer. QuestPDF's Community license is free for organisations under
/// $1M USD/year revenue; larger downstream users must obtain a license. The dependency is isolated
/// behind <see cref="IInvoicePdfRenderer"/> so it can be swapped without touching callers.
/// </summary>
public sealed class InvoicePdfRenderer : IInvoicePdfRenderer
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    static InvoicePdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(InvoiceDto invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Item().Text("INVOICE").FontSize(22).Bold();
                    col.Item().Text(invoice.InvoiceNumber).FontSize(12).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Billed to").SemiBold();
                            c.Item().Text(invoice.TenantId);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Status: {invoice.Status}").SemiBold();
                            c.Item().Text($"Purpose: {invoice.Purpose}");
                            c.Item().Text($"Period: {invoice.PeriodYear}-{invoice.PeriodMonth:00}");
                            if (invoice.PeriodStartUtc is { } ps && invoice.PeriodEndUtc is { } pe)
                            {
                                c.Item().Text($"{ps:yyyy-MM-dd} → {pe:yyyy-MM-dd}").FontColor(Colors.Grey.Darken1);
                            }
                        });
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Issued: {FormatDate(invoice.IssuedAtUtc)}");
                        row.RelativeItem().AlignRight().Text($"Due: {FormatDate(invoice.DueAtUtc)}");
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(5);
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Description").SemiBold();
                            header.Cell().AlignRight().Text("Qty").SemiBold();
                            header.Cell().AlignRight().Text("Unit Price").SemiBold();
                            header.Cell().AlignRight().Text("Amount").SemiBold();
                        });

                        foreach (var line in invoice.LineItems)
                        {
                            table.Cell().Text(line.Description);
                            table.Cell().AlignRight().Text(line.Quantity.ToString("0.##", Culture));
                            table.Cell().AlignRight().Text(line.UnitPrice.ToString("0.00", Culture));
                            table.Cell().AlignRight().Text(line.Amount.ToString("0.00", Culture));
                        }
                    });

                    col.Item().AlignRight()
                        .Text($"Subtotal: {invoice.SubtotalAmount.ToString("0.00", Culture)} {invoice.Currency}")
                        .FontSize(12).Bold();

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        col.Item().PaddingTop(10).Text($"Notes: {invoice.Notes}").FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().AlignCenter()
                    .Text($"Generated {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", Culture)}")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();
    }

    private static string FormatDate(DateTime? value) =>
        value is null ? "—" : value.Value.ToString("yyyy-MM-dd", Culture);
}

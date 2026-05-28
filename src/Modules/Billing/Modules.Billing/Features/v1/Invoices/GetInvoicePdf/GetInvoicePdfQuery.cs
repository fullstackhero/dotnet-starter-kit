using Mediator;

namespace FSH.Modules.Billing.Features.v1.Invoices.GetInvoicePdf;

/// <summary>Fetches the caller-tenant's invoice and renders it to a PDF. Module-internal (the byte[]
/// result is not a cross-module contract).</summary>
public sealed record GetInvoicePdfQuery(Guid InvoiceId) : IQuery<InvoicePdfResult>;

public sealed record InvoicePdfResult(byte[] Content, string FileName);

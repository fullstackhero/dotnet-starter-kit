using Mediator;

namespace FSH.Modules.Billing.Contracts.v1.Invoices;

public sealed record MarkInvoicePaidCommand(Guid InvoiceId) : ICommand<Guid>;

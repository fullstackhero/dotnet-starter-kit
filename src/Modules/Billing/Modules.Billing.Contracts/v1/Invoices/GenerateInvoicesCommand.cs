using Mediator;

namespace FSH.Modules.Billing.Contracts.v1.Invoices;

/// <summary>
/// Admin-triggered invoice generation for a specific billing period across all active tenants.
/// Idempotent: re-running for a period that already has invoices skips those tenants.
/// </summary>
public sealed record GenerateInvoicesCommand(int PeriodYear, int PeriodMonth) : ICommand<int>;

using FSH.Modules.Billing.Contracts.v1.Invoices;
using FSH.Modules.Billing.Services;
using Mediator;

namespace FSH.Modules.Billing.Features.v1.Invoices.GenerateInvoices;

public sealed class GenerateInvoicesCommandHandler(IBillingService billing)
    : ICommandHandler<GenerateInvoicesCommand, int>
{
    public async ValueTask<int> Handle(GenerateInvoicesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await billing.GenerateInvoicesForAllTenantsAsync(command.PeriodYear, command.PeriodMonth, cancellationToken).ConfigureAwait(false);
    }
}

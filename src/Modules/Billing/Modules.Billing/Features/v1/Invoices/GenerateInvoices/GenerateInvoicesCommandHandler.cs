using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts.v1.Invoices;
using FSH.Modules.Billing.Localization;
using FSH.Modules.Billing.Services;
using Mediator;

namespace FSH.Modules.Billing.Features.v1.Invoices.GenerateInvoices;

public sealed class GenerateInvoicesCommandHandler(
    IBillingService billing,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<GenerateInvoicesCommand, int>
{
    public async ValueTask<int> Handle(GenerateInvoicesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Platform-wide invoice generation runs across EVERY tenant — it is a root-operator action.
        // A tenant admin (who also holds Billing.Manage) must not be able to trigger it.
        var callerTenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.")
            {
                MessageKey = "Error.TenantContextRequired",
            };
        if (callerTenantId != MultitenancyConstants.Root.Id)
        {
            throw new ForbiddenException("Only the root operator may generate invoices across tenants.")
            {
                MessageKey = "Billing.OnlyRootOperatorMayGenerateInvoices",
                ResourceSource = typeof(BillingResources),
            };
        }

        return await billing.GenerateInvoicesForAllTenantsAsync(command.PeriodYear, command.PeriodMonth, cancellationToken).ConfigureAwait(false);
    }
}

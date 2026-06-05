using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts.v1.Plans;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Events;
using FSH.Modules.Multitenancy.Contracts.v1.CreateTenant;
using FSH.Modules.Multitenancy.Provisioning;
using Mediator;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Multitenancy.Features.v1.CreateTenant;

public sealed class CreateTenantCommandHandler(
    ITenantService tenantService,
    ITenantProvisioningService provisioningService,
    ITenantInitialPasswordBuffer passwordBuffer,
    IMediator mediator,
    IEventBus events,
    IOptions<TenantBillingOptions> billingOptions,
    TimeProvider timeProvider)
    : ICommandHandler<CreateTenantCommand, CreateTenantCommandResponse>
{
    public async ValueTask<CreateTenantCommandResponse> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Resolve the plan (falls back to trial) and read its term to set the tenant validity
        // window. A bad plan key throws NotFound (400) before any tenant is created.
        var planKey = string.IsNullOrWhiteSpace(command.PlanKey)
            ? billingOptions.Value.DefaultPlanKey
            : command.PlanKey!;
        var term = await mediator.Send(new GetPlanTermQuery(planKey), cancellationToken).ConfigureAwait(false);

        var periodStart = timeProvider.GetUtcNow().UtcDateTime;
        var periodEnd = periodStart.AddMonths(term.TermMonths);

        var tenantId = await tenantService.CreateAsync(
            command.Id,
            command.Name,
            command.ConnectionString,
            command.AdminEmail,
            command.Issuer,
            term.Key,
            periodEnd,
            cancellationToken).ConfigureAwait(false);

        // Buffer the admin password for IdentityDbInitializer's background seed step,
        // storing it before StartAsync so the seed never runs ahead of the buffer.
        passwordBuffer.Store(tenantId, command.AdminPassword);

        var provisioning = await provisioningService.StartAsync(tenantId, cancellationToken).ConfigureAwait(false);

        // Drive the billing side-effects (subscription + term invoice) via an integration event so
        // Multitenancy stays decoupled from the Billing runtime.
        await events.PublishAsync(new TenantSubscribedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: periodStart,
            TenantId: tenantId,
            CorrelationId: provisioning.CorrelationId,
            Source: "Multitenancy",
            PlanId: term.PlanId,
            PlanKey: term.Key,
            PeriodStartUtc: periodStart,
            PeriodEndUtc: periodEnd), cancellationToken).ConfigureAwait(false);

        return new CreateTenantCommandResponse(
            tenantId,
            provisioning.CorrelationId,
            provisioning.Status.ToString());
    }
}

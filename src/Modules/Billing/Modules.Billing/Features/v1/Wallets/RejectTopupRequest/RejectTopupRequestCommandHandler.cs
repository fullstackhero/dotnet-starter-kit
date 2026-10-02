using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.v1.Wallets;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Billing.Features.v1.Wallets.RejectTopupRequest;

public sealed class RejectTopupRequestCommandHandler(
    BillingDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<RejectTopupRequestCommand, Guid>
{
    public async ValueTask<Guid> Handle(RejectTopupRequestCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var callerTenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.")
            {
                MessageKey = "Error.TenantContextRequired",
            };
        var isRoot = callerTenantId == MultitenancyConstants.Root.Id;

        var request = await db.TopupRequests
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Top-up request {command.Id} not found.")
            {
                MessageKey = "Billing.TopupRequestNotFound",
                MessageArgs = [command.Id],
                ResourceSource = typeof(BillingResources),
            };

        if (!isRoot && request.TenantId != callerTenantId)
        {
            throw new UnauthorizedException("You can only reject top-up requests for your own tenant.")
            {
                MessageKey = "Billing.CannotRejectTopupForOtherTenant",
                ResourceSource = typeof(BillingResources),
            };
        }

        if (request.Status != TopupRequestStatus.Pending)
        {
            throw new CustomException(
                $"Top-up request {command.Id} cannot be rejected because it is {request.Status} (only Pending requests can be rejected).",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict)
            {
                MessageKey = "Billing.TopupRequestCannotBeRejected",
                MessageArgs = [command.Id, request.Status],
                ResourceSource = typeof(BillingResources),
            };
        }

        request.Reject(command.Reason);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return request.Id;
    }
}

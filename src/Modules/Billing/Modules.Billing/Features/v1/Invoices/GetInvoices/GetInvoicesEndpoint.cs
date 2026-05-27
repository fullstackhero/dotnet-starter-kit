using FSH.Modules.Billing.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.v1.Invoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Billing.Features.v1.Invoices.GetInvoices;

public static class GetInvoicesEndpoint
{
    internal static RouteHandlerBuilder MapGetInvoicesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/invoices",
                (string? tenantId, InvoiceStatus? status, int? periodYear, int? periodMonth,
                 int pageNumber, int pageSize, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetInvoicesQuery(
                        tenantId,
                        status,
                        periodYear,
                        periodMonth,
                        pageNumber <= 0 ? 1 : pageNumber,
                        pageSize <= 0 ? 20 : pageSize), ct))
            .WithName("GetInvoices")
            .WithSummary("List invoices across all tenants (admin)")
            .RequirePermission(BillingPermissions.View);
    }
}

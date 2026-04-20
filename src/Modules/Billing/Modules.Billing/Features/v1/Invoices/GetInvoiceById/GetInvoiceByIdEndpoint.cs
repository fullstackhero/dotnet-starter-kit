using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Billing.Contracts.v1.Invoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Billing.Features.v1.Invoices.GetInvoiceById;

public static class GetInvoiceByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetInvoiceByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/invoices/{invoiceId:guid}",
                (Guid invoiceId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetInvoiceByIdQuery(invoiceId), ct))
            .WithName("GetInvoiceById")
            .WithSummary("Get a single invoice by id")
            .RequirePermission(IdentityPermissionConstants.Billing.View);
    }
}

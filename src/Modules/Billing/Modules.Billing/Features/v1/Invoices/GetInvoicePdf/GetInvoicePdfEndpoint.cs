using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Billing.Contracts.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Billing.Features.v1.Invoices.GetInvoicePdf;

public static class GetInvoicePdfEndpoint
{
    internal static RouteHandlerBuilder MapGetInvoicePdfEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/invoices/{invoiceId:guid}/pdf",
                async (Guid invoiceId, IMediator mediator, CancellationToken ct) =>
                {
                    var result = await mediator.Send(new GetInvoicePdfQuery(invoiceId), ct).ConfigureAwait(false);
                    return Results.File(result.Content, "application/pdf", result.FileName);
                })
            .WithName("GetInvoicePdf")
            .WithSummary("Download an invoice as a PDF")
            // BillingPermissions.View is basic (granted to tenant users), and the handler scopes to the
            // caller's tenant — so this single endpoint serves both operators and tenant self-service.
            .RequirePermission(BillingPermissions.View)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status404NotFound);
    }
}

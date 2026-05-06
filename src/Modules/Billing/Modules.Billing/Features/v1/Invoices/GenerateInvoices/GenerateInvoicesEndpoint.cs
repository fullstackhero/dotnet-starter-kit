using FSH.Modules.Billing.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Billing.Contracts.v1.Invoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Billing.Features.v1.Invoices.GenerateInvoices;

public static class GenerateInvoicesEndpoint
{
    internal static RouteHandlerBuilder MapGenerateInvoicesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/invoices/generate",
                async (GenerateInvoicesCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(new { generated = await mediator.Send(command, ct) }))
            .WithName("GenerateInvoices")
            .WithSummary("Manually trigger invoice generation for a period")
            .RequirePermission(BillingPermissions.Manage)
            .WithIdempotency();
    }
}

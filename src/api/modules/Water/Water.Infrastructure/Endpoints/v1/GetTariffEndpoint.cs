using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Tariffs.Get.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class GetTariffEndpoint
{
    internal static RouteHandlerBuilder MapGetTariffEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                var response = await mediator.Send(new GetTariffRequest(id));
                return Results.Ok(response);
            })
            .WithName(nameof(GetTariffEndpoint))
            .WithSummary("gets tariff by id")
            .WithDescription("gets tariff by id")
            .Produces<TariffResponse>()
            .RequirePermission("Permissions.Tariffs.View")
            .MapToApiVersion(1);
    }
}

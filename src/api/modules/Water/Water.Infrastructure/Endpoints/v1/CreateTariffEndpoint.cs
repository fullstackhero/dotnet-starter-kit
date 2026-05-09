using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Tariffs.Create.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class CreateTariffEndpoint
{
    internal static RouteHandlerBuilder MapTariffCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/", async (CreateTariffCommand request, ISender mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(CreateTariffEndpoint))
            .WithSummary("creates a tariff")
            .WithDescription("creates a tariff")
            .Produces<CreateTariffResponse>()
            .RequirePermission("Permissions.Tariffs.Create")
            .MapToApiVersion(1);
    }
}

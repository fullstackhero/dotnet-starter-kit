using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Tariffs.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class UpdateTariffEndpoint
{
    internal static RouteHandlerBuilder MapTariffUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/{id:guid}", async (Guid id, UpdateTariffCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateTariffEndpoint))
            .WithSummary("update a tariff")
            .WithDescription("update a tariff")
            .Produces<UpdateTariffResponse>()
            .RequirePermission("Permissions.Tariffs.Update")
            .MapToApiVersion(1);
    }
}

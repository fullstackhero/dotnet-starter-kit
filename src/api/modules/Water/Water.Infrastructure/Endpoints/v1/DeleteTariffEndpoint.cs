using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Tariffs.Delete.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class DeleteTariffEndpoint
{
    internal static RouteHandlerBuilder MapTariffDeleteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
             {
                 await mediator.Send(new DeleteTariffCommand(id));
                 return Results.NoContent();
             })
            .WithName(nameof(DeleteTariffEndpoint))
            .WithSummary("deletes tariff by id")
            .WithDescription("deletes tariff by id")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.Tariffs.Delete")
            .MapToApiVersion(1);
    }
}

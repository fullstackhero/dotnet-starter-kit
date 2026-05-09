using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Water.Application.Meters.Update.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Water.Infrastructure.Endpoints.v1;
public static class UpdateMeterEndpoint
{
    internal static RouteHandlerBuilder MapMeterUpdateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/{id:guid}", async (Guid id, UpdateMeterCommand request, ISender mediator) =>
            {
                if (id != request.Id) return Results.BadRequest();
                var response = await mediator.Send(request);
                return Results.Ok(response);
            })
            .WithName(nameof(UpdateMeterEndpoint))
            .WithSummary("update a meter")
            .WithDescription("update a meter")
            .Produces<UpdateMeterResponse>()
            .RequirePermission("Permissions.Meters.Update")
            .MapToApiVersion(1);
    }
}

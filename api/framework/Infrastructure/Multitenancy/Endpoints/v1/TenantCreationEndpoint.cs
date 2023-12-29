using FSH.Framework.Core.MultiTenancy.Features.Creation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Multitenancy.Endpoints.v1;
public static class TenantCreationEndpoint
{
    internal static RouteHandlerBuilder MapTenantCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", (TenantCreationCommand request, ISender mediator) => mediator.Send(request))
                        .WithName(nameof(TenantCreationEndpoint))
                        .WithSummary("creates a tenant")
                        .WithDescription("creates a tenant")
                        .MapToApiVersion(1.0);
    }
}

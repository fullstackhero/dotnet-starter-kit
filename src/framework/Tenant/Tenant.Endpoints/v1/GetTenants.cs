using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Infrastructure.Tenant.Endpoints;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Tenant.Core.Abstractions;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Tenant.Endpoints.v1;
public static class GetTenants
{
    public sealed class Query : IQuery<Response>;
    public sealed class Response
    {
        public IReadOnlyCollection<GetTenantById.Response> Tenants { get; init; } = [];
    }
    public static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/", (IQueryDispatcher dispatcher) => dispatcher.SendAsync<Query, Response>(new Query()))
                                .WithName(nameof(GetTenants))
                                .WithSummary("get tenants")
                                .RequirePermission("Permissions.Tenants.View")
                                .WithDescription("get tenants");
    }
    public sealed class GetTenantsHandler(ITenantService service) : IQueryHandler<Query, Response>
    {
        public async Task<Response> HandleAsync(Query request, CancellationToken cancellationToken = default)
        {
            var tenants = await service.GetAllAsync();
            return new Response
            {
                Tenants = tenants.Adapt<List<GetTenantById.Response>>()
            };
        }
    }

}

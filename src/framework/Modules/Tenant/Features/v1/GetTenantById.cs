using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Tenant.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Tenant.Features.v1;
public static class GetTenantById
{
    public sealed record Query(string TenantId) : IQuery<Response>;
    public class Response
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? ConnectionString { get; set; }
        public string AdminEmail { get; set; } = default!;
        public bool IsActive { get; set; }
        public DateTime ValidUpto { get; set; }
        public string? Issuer { get; set; }
    }
    internal static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/{id}", (IQueryDispatcher dispatcher, string id)
            => dispatcher.SendAsync(new Query(id)))
                                .WithName(nameof(GetTenantById))
                                .WithSummary("get tenant by id")
                                .RequirePermission("Permissions.Tenants.View")
                                .WithDescription("get tenant by id");
    }
    public sealed class Handler(ITenantService service, IQueryDispatcher dispatcher) : IQueryHandler<Query, Response>
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken = default)
        {

            var data = await dispatcher.SendAsync(new Query(query.TenantId), cancellationToken);
            var tenant = await service.GetByIdAsync(query.TenantId);
            return new Response()
            {
                Id = tenant.Id,
                Name = tenant.Name,
                ConnectionString = tenant.ConnectionString,
                AdminEmail = tenant.AdminEmail,
                IsActive = tenant.IsActive,
                ValidUpto = tenant.ValidUpto,
                Issuer = tenant.Issuer,
            };
        }
    }

}

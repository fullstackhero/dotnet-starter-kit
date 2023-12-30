using Carter;
using FSH.Framework.Infrastructure.Multitenancy.Endpoints.v1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Multitenancy;
public sealed class MutitenancyModule
{
    public class Endpoints : CarterModule
    {
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            var tenantGroup = app.MapGroup("tenants").WithTags("tenants");
            tenantGroup.MapTenantCreationEndpoint();
            tenantGroup.MapGetTenantListEndpoint();
        }
    }
}

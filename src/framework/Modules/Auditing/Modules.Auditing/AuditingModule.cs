using Asp.Versioning;
using FSH.Framework.Auditing.Data;
using FSH.Framework.Auditing.Features.v1.GetUserTrails;
using FSH.Framework.Auditing.Services;
using FSH.Framework.Infrastructure.Messaging.CQRS;
using FSH.Framework.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Auditing.Endpoints;
public static class AuditingModule
{
    public static IServiceCollection ConfigureAuditingModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAuditingDbContext>(provider => provider.GetRequiredService<AuditingDbContext>());
        services.RegisterCommandAndQueryHandlers(typeof(AuditingModule).Assembly);
        services.AddScoped<IAuditService, AuditService>();
        services.BindDbContext<AuditingDbContext>();
        return services;
    }

    public static IEndpointRouteBuilder MapAuditingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/auditing")
            .WithTags("Auditing")
            .WithOpenApi()
            .WithApiVersionSet(apiVersionSet);

        GetUserTrailsEndpoint.Map(group);

        return endpoints;
    }
}
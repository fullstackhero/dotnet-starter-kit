using Asp.Versioning;
using FSH.Framework.Auditing.Data;
using FSH.Framework.Auditing.Features.v1.GetUserTrails;
using FSH.Framework.Auditing.Services;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Messaging.CQRS;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Modules.Common.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Auditing;
public class AuditingModule : IModule
{
    public void AddModule(IServiceCollection services, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAuditingDbContext>(provider => provider.GetRequiredService<AuditingDbContext>());
        services.RegisterCommandAndQueryHandlers(typeof(AuditingModule).Assembly);
        services.AddScoped<IAuditService, AuditService>();
        services.BindDbContext<AuditingDbContext>();
        services.AddScoped<IDbInitializer, AuditingDbInitializer>();
    }

    public void ConfigureModule(WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = app
            .MapGroup("api/v{version:apiVersion}/auditing")
            .WithTags("Auditing")
            .WithOpenApi()
            .WithApiVersionSet(apiVersionSet);

        group.Map();
    }
}
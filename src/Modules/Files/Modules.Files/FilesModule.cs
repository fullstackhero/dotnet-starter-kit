using FSH.Framework.Persistence;
using FSH.Framework.Web.Modules;
using FSH.Modules.Files.Data;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Files.FilesModule), 350)]

namespace FSH.Modules.Files;

/// <summary>
/// Files module: presigned-URL file lifecycle (upload, finalize, serve, delete, restore, purge)
/// shared across the kit's owning features (Catalog product images, Ticket attachments, My Files,
/// avatars, tenant logos). Module order 350 places it between Auditing (300) and Webhooks (400);
/// owning modules (Catalog=600, Tickets=700) load later and register their IFileAccessPolicy
/// implementations during their own ConfigureServices.
///
/// Phase A: skeleton + DbContext only. Endpoints, scanner, access policies, and jobs are wired
/// in Phase A.2 (see docs/superpowers/plans/2026-05-12-files-module-phase-a.md, Tasks 12+).
/// </summary>
public sealed class FilesModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<FilesOptions>(builder.Configuration.GetSection("Files"));
        builder.Services.AddHeroDbContext<FilesDbContext>();
        builder.Services.AddScoped<IDbInitializer, FilesDbInitializer>();

        builder.Services.AddHealthChecks().AddDbContextCheck<FilesDbContext>(
            name: "db:files",
            failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        // Endpoints land in Phase A.2 — see plan Tasks 18-21.
    }
}

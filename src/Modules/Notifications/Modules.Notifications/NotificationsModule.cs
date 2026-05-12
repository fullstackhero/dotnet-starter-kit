using Asp.Versioning;
using FluentValidation;
using FSH.Framework.Eventing;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Notifications.Contracts.Authorization;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Features.v1.GetUnreadCount;
using FSH.Modules.Notifications.Features.v1.ListNotifications;
using FSH.Modules.Notifications.Features.v1.MarkAllNotificationsRead;
using FSH.Modules.Notifications.Features.v1.MarkNotificationRead;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.Notifications;

/// <summary>
/// Notifications module: per-user inbox driven by integration events from other modules. Module
/// Order 750 places it BEFORE Chat (800) so its integration-event handlers are registered
/// before Chat starts publishing — handler registration is order-sensitive.
/// </summary>
public sealed class NotificationsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(NotificationPermissions.All);

        builder.Services.AddHeroDbContext<NotificationsDbContext>();
        builder.Services.AddScoped<IDbInitializer, NotificationsDbInitializer>();
        builder.Services.AddValidatorsFromAssembly(typeof(NotificationsModule).Assembly);

        // Subscribe to cross-module integration events handled by this assembly.
        builder.Services.AddIntegrationEventHandlers(typeof(NotificationsModule).Assembly);

        builder.Services.AddHealthChecks().AddDbContextCheck<NotificationsDbContext>(
            name: "db:notifications",
            failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("api/v{version:apiVersion}/notifications")
            .WithTags("Notifications")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Literal routes first; /{id:guid}/read is the only param-route and lives last.
        group.MapListNotificationsEndpoint();              // GET /
        group.MapGetUnreadCountEndpoint();                 // GET /unread-count
        group.MapMarkAllNotificationsReadEndpoint();       // POST /read-all
        group.MapMarkNotificationReadEndpoint();           // POST /{id:guid}/read
    }
}

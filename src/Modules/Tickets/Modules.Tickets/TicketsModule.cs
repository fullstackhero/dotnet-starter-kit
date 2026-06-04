using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Data;
using FSH.Modules.Tickets.Features.v1.Tickets.AddTicketComment;
using FSH.Modules.Tickets.Features.v1.Tickets.AssignTicket;
using FSH.Modules.Tickets.Features.v1.Tickets.CloseTicket;
using FSH.Modules.Tickets.Features.v1.Tickets.CreateTicket;
using FSH.Modules.Tickets.Features.v1.Tickets.DeleteTicket;
using FSH.Modules.Tickets.Features.v1.Tickets.GetTicketById;
using FSH.Modules.Tickets.Features.v1.Tickets.ListTicketComments;
using FSH.Modules.Tickets.Features.v1.Tickets.ListTrashedTickets;
using FSH.Modules.Tickets.Features.v1.Tickets.ReopenTicket;
using FSH.Modules.Tickets.Features.v1.Tickets.ResolveTicket;
using FSH.Modules.Tickets.Features.v1.Tickets.RestoreTicket;
using FSH.Modules.Tickets.Features.v1.Tickets.SearchTickets;
using FSH.Modules.Tickets.Features.v1.Tickets.UpdateTicket;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Tickets.TicketsModule), 700)]

namespace FSH.Modules.Tickets;

public sealed class TicketsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(TicketsPermissions.All);

        builder.Services.AddHeroDbContext<TicketsDbContext>();
        builder.Services.AddScoped<IDbInitializer, TicketsDbInitializer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<TicketsDbContext>(
                name: "db:tickets",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        // No custom middleware needed
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}")
            .WithTags("Tickets")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Trash + comment routes registered before the catch-all
        // `{ticketId:guid}` GET so the literal segments win. Minimal APIs
        // match the first compatible pattern, so order matters.
        group.MapListTrashedTicketsEndpoint();
        group.MapAddTicketCommentEndpoint();
        group.MapListTicketCommentsEndpoint();

        group.MapRestoreTicketEndpoint();
        group.MapAssignTicketEndpoint();
        group.MapResolveTicketEndpoint();
        group.MapReopenTicketEndpoint();
        group.MapCloseTicketEndpoint();

        group.MapCreateTicketEndpoint();
        group.MapSearchTicketsEndpoint();
        group.MapUpdateTicketEndpoint();
        group.MapDeleteTicketEndpoint();
        group.MapGetTicketByIdEndpoint();
    }
}

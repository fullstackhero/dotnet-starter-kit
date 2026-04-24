using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Web.Modules;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Features.v1.Invoices.GenerateInvoices;
using FSH.Modules.Billing.Features.v1.Invoices.GetInvoiceById;
using FSH.Modules.Billing.Features.v1.Invoices.GetInvoices;
using FSH.Modules.Billing.Features.v1.Invoices.GetMyInvoices;
using FSH.Modules.Billing.Features.v1.Invoices.IssueInvoice;
using FSH.Modules.Billing.Features.v1.Invoices.MarkInvoicePaid;
using FSH.Modules.Billing.Features.v1.Invoices.VoidInvoice;
using FSH.Modules.Billing.Features.v1.Plans.CreatePlan;
using FSH.Modules.Billing.Features.v1.Plans.GetPlans;
using FSH.Modules.Billing.Features.v1.Plans.UpdatePlan;
using FSH.Modules.Billing.Features.v1.Subscriptions.AssignSubscription;
using FSH.Modules.Billing.Features.v1.Subscriptions.GetSubscription;
using FSH.Modules.Billing.Features.v1.Usage.CaptureUsageSnapshots;
using FSH.Modules.Billing.Features.v1.Usage.GetUsageSnapshots;
using FSH.Modules.Billing.Services;
using Hangfire;
using Hangfire.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Billing.BillingModule), 500)]

namespace FSH.Modules.Billing;

public sealed class BillingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHeroDbContext<BillingDbContext>();
        builder.Services.AddScoped<IDbInitializer, BillingDbInitializer>();
        builder.Services.AddScoped<IUsageReporter, UsageReporter>();
        builder.Services.AddScoped<IBillingService, BillingService>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<BillingDbContext>(
                name: "db:billing",
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
            .MapGroup("api/v{version:apiVersion}/billing")
            .WithTags("Billing")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        group.MapGetPlansEndpoint();
        group.MapCreatePlanEndpoint();
        group.MapUpdatePlanEndpoint();

        group.MapGetSubscriptionEndpoint();
        group.MapGetMySubscriptionEndpoint();
        group.MapAssignSubscriptionEndpoint();

        group.MapGetInvoicesEndpoint();
        group.MapGetMyInvoicesEndpoint();
        group.MapGetInvoiceByIdEndpoint();
        group.MapGenerateInvoicesEndpoint();
        group.MapIssueInvoiceEndpoint();
        group.MapMarkInvoicePaidEndpoint();
        group.MapVoidInvoiceEndpoint();

        group.MapGetUsageSnapshotsEndpoint();
        group.MapCaptureUsageSnapshotsEndpoint();

        var jobManager = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
        if (jobManager is not null)
        {
            // Fire at 00:05 UTC on the 1st of every month; the job bills the previous period.
            jobManager.AddOrUpdate(
                "billing-monthly-invoices",
                Job.FromExpression<MonthlyInvoiceJob>(j => j.RunAsync(CancellationToken.None)),
                "5 0 1 * *",
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
    }
}

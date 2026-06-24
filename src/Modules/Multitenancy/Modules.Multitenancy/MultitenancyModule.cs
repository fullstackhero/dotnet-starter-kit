using Asp.Versioning;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Finbuckle.MultiTenant.Extensions;
using Finbuckle.MultiTenant.Stores;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Modules;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Features.v1.AdjustTenantValidity;
using FSH.Modules.Multitenancy.Features.v1.ChangeTenantActivation;
using FSH.Modules.Multitenancy.Features.v1.CreateTenant;
using FSH.Modules.Multitenancy.Features.v1.GetMyTenantStatus;
using FSH.Modules.Multitenancy.Features.v1.GetTenantMigrations;
using FSH.Modules.Multitenancy.Features.v1.GetTenants;
using FSH.Modules.Multitenancy.Features.v1.GetTenantStatus;
using FSH.Modules.Multitenancy.Features.v1.GetTenantTheme;
using FSH.Modules.Multitenancy.Features.v1.ResetTenantTheme;
using FSH.Modules.Multitenancy.Features.v1.TenantProvisioning.GetTenantProvisioningStatus;
using FSH.Modules.Multitenancy.Features.v1.TenantProvisioning.RetryTenantProvisioning;
using FSH.Modules.Multitenancy.Features.v1.RenewTenant;
using FSH.Modules.Multitenancy.Features.v1.UpdateTenantTheme;
using FSH.Modules.Multitenancy.Provisioning;
using FSH.Modules.Multitenancy.Services;
using Hangfire;
using Hangfire.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace FSH.Modules.Multitenancy;

public sealed class MultitenancyModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        FSH.Framework.Shared.Constants.PermissionConstants.Register(
            FSH.Modules.Multitenancy.Contracts.Authorization.MultitenancyPermissions.All);

        builder.Services.Configure<TenantBillingOptions>(
            builder.Configuration.GetSection(TenantBillingOptions.SectionName));

        builder.Services.AddScoped<ITenantService, TenantService>();
        builder.Services.AddScoped<ITenantThemeService, TenantThemeService>();
        builder.Services.AddTransient<IConnectionStringValidator, ConnectionStringValidator>();
        builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        builder.Services.AddTransient<TenantProvisioningJob>();
        builder.Services.AddTransient<TenantExpiryScanJob>();

        // Singleton — the buffer survives the request scope that calls Store(...)
        // so the background Hangfire-scheduled seed scope can still TryConsume(...).
        builder.Services.AddSingleton<
            FSH.Framework.Shared.Multitenancy.ITenantInitialPasswordBuffer,
            Services.TenantInitialPasswordBuffer>();

        builder.Services.AddHeroDbContext<TenantDbContext>();

        // Replace (not Add) the no-op event tenant scope with a Finbuckle-backed one so background
        // event dispatch establishes the tenant before tenant-filtered handler DbContexts are built.
        builder.Services.Replace(
            ServiceDescriptor.Singleton<IEventTenantScope, FinbuckleEventTenantScope>());

        builder.Services
            .AddMultiTenant<AppTenantInfo>(options =>
            {
                options.Events.OnTenantResolveCompleted = async context =>
                {
                    if (context.MultiTenantContext.StoreInfo is null) return;
                    if (context.MultiTenantContext.StoreInfo.StoreType != typeof(DistributedCacheStore<AppTenantInfo>))
                    {
                        var sp = ((HttpContext)context.Context!).RequestServices;
                        var distributedStore = sp
                            .GetRequiredService<IEnumerable<IMultiTenantStore<AppTenantInfo>>>()
                            .FirstOrDefault(s => s.GetType() == typeof(DistributedCacheStore<AppTenantInfo>));

                        await distributedStore!.AddAsync(context.MultiTenantContext.TenantInfo!);
                    }
                    await Task.CompletedTask;
                };
            })
            // ── Strategy chain — first non-null identifier wins (registration order) ──
            // ClaimStrategy no-ops here: UseMultiTenant() runs BEFORE UseAuthentication(), so User is
            // anonymous at resolution. Tenant stays header-driven; root override is post-auth middleware below.
            .WithClaimStrategy(ClaimConstants.Tenant)
            .WithHeaderStrategy(MultitenancyConstants.Identifier)
            .WithDelegateStrategy(async context =>
            {
                if (context is not HttpContext httpContext) return null;

                if (!httpContext.Request.Query.TryGetValue("tenant", out var tenantIdentifier) ||
                    string.IsNullOrEmpty(tenantIdentifier))
                    return null;

                return await Task.FromResult(tenantIdentifier.ToString());
            })
            .WithDistributedCacheStore(TimeSpan.FromMinutes(60))
            .WithStore<EFCoreStore<TenantDbContext, AppTenantInfo>>(ServiceLifetime.Scoped);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<TenantDbContext>(
                name: "db:multitenancy",
                failureStatus: HealthStatus.Unhealthy)
            .AddCheck<TenantMigrationsHealthCheck>(
                name: "db:tenants-migrations",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // ── Root-operator header override ──────────────────────────────
        // A "root"-claim caller scopes one request to another tenant via the `tenant` header (post-auth, since
        // Finbuckle's pre-auth chain has no User). Gated on claim==root + header set != root + target exists.
        app.Use(async (ctx, next) =>
        {
            var callerTenant = ctx.User?.FindFirstValue(ClaimConstants.Tenant);
            if (string.Equals(callerTenant, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
            {
                var headerValue = ctx.Request.Headers[MultitenancyConstants.Identifier].FirstOrDefault();
                if (!string.IsNullOrEmpty(headerValue) &&
                    !string.Equals(headerValue, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
                {
                    var store = ctx.RequestServices.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
                    var target = await store.GetAsync(headerValue).ConfigureAwait(false);
                    if (target is not null)
                    {
                        var setter = ctx.RequestServices.GetRequiredService<IMultiTenantContextSetter>();
                        setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(target);
                    }
                }
            }
            await next(ctx).ConfigureAwait(false);
        });

        // ── Deactivated-tenant guard ───────────────────────────────────
        // Finbuckle resolves inactive tenants normally, so this post-auth guard rejects any request (incl.
        // anonymous login/refresh) with a non-root inactive tenant; root operators are exempt.
        app.Use(async (ctx, next) =>
        {
            var callerTenant = ctx.User?.FindFirstValue(ClaimConstants.Tenant);
            var isOperator = string.Equals(callerTenant, MultitenancyConstants.Root.Id, StringComparison.Ordinal);
            if (!isOperator)
            {
                var accessor = ctx.RequestServices.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
                var tenant = accessor.MultiTenantContext?.TenantInfo;

                // Claim strategy no-ops pre-auth, so a JWT-only (no header) request may have no resolved
                // tenant here — fall back to the caller's claim.
                if (tenant is null && !string.IsNullOrEmpty(callerTenant))
                {
                    var store = ctx.RequestServices.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
                    tenant = await store.GetAsync(callerTenant).ConfigureAwait(false);
                }

                if (tenant is not null &&
                    !string.Equals(tenant.Id, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
                {
                    if (!tenant.IsActive)
                    {
                        throw new ForbiddenException("This tenant has been deactivated. Contact your administrator.");
                    }

                    // Expiry is enforced on every request (not just at login) with a grace period:
                    // a tenant past ValidUpto still works until ValidUpto + grace, then is hard-blocked.
                    var graceDays = ctx.RequestServices
                        .GetRequiredService<IOptions<TenantBillingOptions>>().Value.GracePeriodDays;
                    var nowUtc = ctx.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
                    var graceEndsUtc = tenant.ValidUpto.AddDays(graceDays);
                    if (nowUtc > graceEndsUtc)
                    {
                        throw new ForbiddenException("This tenant's subscription has expired. Please renew to continue.");
                    }

                    // Inside the grace period: surface days-left so clients can warn. Set via OnStarting so
                    // the header survives even when an exception handler rewrites the response.
                    if (nowUtc > tenant.ValidUpto)
                    {
                        var daysLeft = (int)Math.Ceiling((graceEndsUtc - nowUtc).TotalDays);
                        var headerValue = daysLeft.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        ctx.Response.OnStarting(static state =>
                        {
                            var (response, value) = ((HttpResponse, string))state;
                            response.Headers["X-Subscription-Grace"] = value;
                            return Task.CompletedTask;
                        }, (ctx.Response, headerValue));
                    }
                }
            }

            await next(ctx).ConfigureAwait(false);
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("api/v{version:apiVersion}/tenants")
            .WithTags("Tenants")
            .WithApiVersionSet(versionSet);
        ChangeTenantActivationEndpoint.Map(group);
        GetTenantsEndpoint.Map(group);
        RenewTenantEndpoint.Map(group);
        AdjustTenantValidityEndpoint.Map(group);
        CreateTenantEndpoint.Map(group);
        GetTenantStatusEndpoint.Map(group);
        GetMyTenantStatusEndpoint.Map(group);
        GetTenantProvisioningStatusEndpoint.Map(group);
        RetryTenantProvisioningEndpoint.Map(group);
        TenantMigrationsEndpoint.Map(group);

        // Theme endpoints
        GetTenantThemeEndpoint.Map(group);
        UpdateTenantThemeEndpoint.Map(group);
        ResetTenantThemeEndpoint.Map(group);

        var jobManager = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
        if (jobManager is not null)
        {
            // Scan tenants daily at 02:00 UTC; publishes nearing-expiry / entered-grace / expired notices.
            jobManager.AddOrUpdate(
                "tenant-expiry-scan",
                Job.FromExpression<TenantExpiryScanJob>(j => j.RunAsync(CancellationToken.None)),
                "0 2 * * *",
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
    }
}
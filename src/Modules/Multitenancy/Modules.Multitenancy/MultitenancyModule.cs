using Asp.Versioning;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Finbuckle.MultiTenant.Extensions;
using Finbuckle.MultiTenant.Stores;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Modules;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Features.v1.ChangeTenantActivation;
using FSH.Modules.Multitenancy.Features.v1.CreateTenant;
using FSH.Modules.Multitenancy.Features.v1.GetTenants;
using FSH.Modules.Multitenancy.Features.v1.GetTenantStatus;
using FSH.Modules.Multitenancy.Features.v1.GetTenantTheme;
using FSH.Modules.Multitenancy.Features.v1.ResetTenantTheme;
using FSH.Modules.Multitenancy.Features.v1.TenantProvisioning.GetTenantProvisioningStatus;
using FSH.Modules.Multitenancy.Features.v1.TenantProvisioning.RetryTenantProvisioning;
using FSH.Modules.Multitenancy.Features.v1.UpdateTenantTheme;
using FSH.Modules.Multitenancy.Features.v1.UpgradeTenant;
using FSH.Modules.Multitenancy.Provisioning;
using FSH.Modules.Multitenancy.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace FSH.Modules.Multitenancy;

public sealed class MultitenancyModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        FSH.Framework.Shared.Constants.PermissionConstants.Register(
            FSH.Modules.Multitenancy.Contracts.Authorization.MultitenancyPermissions.All);

        builder.Services.AddScoped<ITenantService, TenantService>();
        builder.Services.AddScoped<ITenantThemeService, TenantThemeService>();
        builder.Services.AddTransient<IConnectionStringValidator, ConnectionStringValidator>();
        builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        builder.Services.AddTransient<TenantProvisioningJob>();

        // Singleton — the buffer survives the request scope that calls Store(...)
        // so the background Hangfire-scheduled seed scope can still TryConsume(...).
        builder.Services.AddSingleton<
            FSH.Framework.Shared.Multitenancy.ITenantInitialPasswordBuffer,
            Services.TenantInitialPasswordBuffer>();

        builder.Services.AddHeroDbContext<TenantDbContext>();

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
            // ── Strategy chain — first hit wins ────────────────────────
            // Finbuckle runs strategies in registration order, first non-null
            // identifier wins. ClaimStrategy reads HttpContext.User.Claims, so
            // it only works when auth has already populated User. In FSH the
            // app pipeline runs UseMultiTenant() BEFORE UseAuthentication(),
            // so at strategy-resolution time User is still anonymous and
            // ClaimStrategy effectively no-ops. That's intentional: tenant
            // resolution stays header-driven, and the root-operator header
            // override is implemented as a post-auth middleware below
            // (see ConfigureMiddleware) where User claims are available.
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
        // Lets a SuperAdmin (caller whose JWT tenant claim is "root") scope
        // a single request to another tenant by sending the `tenant` header
        // (e.g. admin app searching users in tenant X before impersonation).
        //
        // Implemented as a post-auth middleware (registered via
        // UseModuleMiddlewares, which runs AFTER UseAuthentication) because
        // Finbuckle's strategy chain runs BEFORE auth — User.Claims is empty
        // at strategy-resolution time. By the time this middleware runs,
        // Finbuckle has already resolved a tenant (root, from the matching
        // header sent by the admin app) and JWT bearer has populated User.
        // We re-resolve only when the override conditions are satisfied:
        //   - caller's claim tenant == "root"  (gate: prevents cross-tenant escape)
        //   - request `tenant` header is set and != "root"
        //   - the requested tenant exists in the store
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
        UpgradeTenantEndpoint.Map(group);
        CreateTenantEndpoint.Map(group);
        GetTenantStatusEndpoint.Map(group);
        GetTenantProvisioningStatusEndpoint.Map(group);
        RetryTenantProvisioningEndpoint.Map(group);

        // Theme endpoints
        GetTenantThemeEndpoint.Map(group);
        UpdateTenantThemeEndpoint.Map(group);
        ResetTenantThemeEndpoint.Map(group);
    }
}
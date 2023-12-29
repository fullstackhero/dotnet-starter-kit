using FSH.Framework.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Multitenancy;
internal static class Extensions
{
    public static IServiceCollection AddMultitenancy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.BindDbContext<TenantDbContext>();
        services.AddMultiTenant<FshTenantInfo>()
            .WithHostStrategy()
            //.WithClaimStrategy(MultitenancyConstants.Identifier)
            .WithHeaderStrategy(MultitenancyConstants.Identifier)
            .WithEFCoreStore<TenantDbContext, FshTenantInfo>();
        return services;
    }

    public static IApplicationBuilder UseFshMultitenancy(this IApplicationBuilder app)
    {

        ArgumentNullException.ThrowIfNull(app);
        app.UseMultiTenant();
        app.EnsureMigrations<TenantDbContext>();
        return app;
    }
}

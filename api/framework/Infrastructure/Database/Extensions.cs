using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace FSH.Framework.Infrastructure.Database;
public static class Extensions
{
    private static readonly ILogger _logger = Log.ForContext(typeof(Extensions));
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOptions<DbConfig>()
            .BindConfiguration(nameof(DbConfig))
            .PostConfigure(config =>
            {
                if (config.UseInMemoryDb)
                {
                    _logger.Information("using in-memory database..");
                }

            });
        return builder;
    }

    public static IServiceCollection BindDbContext<T>(this IServiceCollection services) where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDbContext<T>((p, options) =>
        {
            var dbConfig = p.GetRequiredService<IOptions<DbConfig>>().Value;
            if (dbConfig.UseInMemoryDb)
            {
                options.UseInMemoryDatabase(nameof(T).ToUpperInvariant().Replace("DBCONTEXT", "", StringComparison.InvariantCultureIgnoreCase));
            }
        });
        return services;
    }
}

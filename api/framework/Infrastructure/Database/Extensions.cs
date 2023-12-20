using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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
}

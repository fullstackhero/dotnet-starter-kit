using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FSH.WebApi.Utils.SourceGenerator;
using Serilog;
using FSH.WebApi.Infrastructure.Common;
using FSH.WebApi.Infrastructure.Persistence.Context;

namespace FSH.WebApi.Utils.SourceGenerator;

internal static class Startup
{

    private static readonly ILogger _logger = Log.ForContext(typeof(Startup));
    internal static IServiceCollection AddGenerateSources(this IServiceCollection services, IConfiguration config) =>
services.Configure<GenerateSourcesSettings>(config.GetSection(nameof(GenerateSourcesSettings)));

    internal static IServiceCollection AddSourceGenerator(this IServiceCollection services, IConfiguration config)
    {
        return services
        .AddTransient<IGenerateSources, GenerateSources>();
    }
}
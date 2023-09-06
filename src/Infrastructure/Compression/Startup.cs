using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;

namespace FSH.WebApi.Infrastructure.Compression;

internal static class Startup
{
    internal static IServiceCollection AddCompressions(this IServiceCollection services)
    {
        // Add Response compression services
        services.AddResponseCompression(options =>
         {
             options.EnableForHttps = true;
             options.Providers.Add<GzipCompressionProvider>();
         });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        return services;
    }

    internal static IApplicationBuilder UseCompressions(this IApplicationBuilder app) =>
        app.UseResponseCompression();
}
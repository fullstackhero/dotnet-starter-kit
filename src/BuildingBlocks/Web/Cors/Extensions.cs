using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using AspNetCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace FSH.Framework.Web.Cors;

public static class Extensions
{
    private const string PolicyName = "FSHCorsPolicy";

    public static IServiceCollection AddHeroCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(nameof(CorsOptions)))
            .Validate(settings => settings.AllowAll || settings.AllowedOrigins.Length > 0, "CorsOptions: AllowedOrigins are required when AllowAll is false.")
            .Validate(settings => settings.AllowAll || settings.AllowedHeaders.Length > 0, "CorsOptions: AllowedHeaders are required when AllowAll is false.")
            .Validate(settings => settings.AllowAll || settings.AllowedMethods.Length > 0, "CorsOptions: AllowedMethods are required when AllowAll is false.")
            .ValidateOnStart();

        services.AddCors();
        services.AddSingleton<IConfigureOptions<AspNetCorsOptions>>(sp =>
        {
            var corsSettings = sp.GetRequiredService<IOptions<CorsOptions>>();
            return new ConfigureOptions<AspNetCorsOptions>(options =>
            {
                options.AddPolicy(PolicyName, builder =>
                {
                    var settings = corsSettings.Value;
                    if (settings.AllowAll)
                    {
                        // Echo the request origin (not `*`): the CORS spec forbids `*` with credentialed requests,
                        // and SignalR's negotiate always runs credentialed — so AllowCredentials needs a specific origin.
                        builder
                            .SetIsOriginAllowed(_ => true)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                    else
                    {
                        builder
                            .WithOrigins(settings.AllowedOrigins)
                            .WithHeaders(settings.AllowedHeaders)
                            .WithMethods(settings.AllowedMethods)
                            .AllowCredentials();
                    }
                });
            });
        });

        return services;
    }

    public static void UseHeroCors(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseCors(PolicyName);
    }
}
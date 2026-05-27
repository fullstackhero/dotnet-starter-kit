using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace FSH.Framework.Web.OpenApi;

public static class Extensions
{
    /// <summary>
    /// Registers OpenAPI documents per API version. Each version gets a separate document
    /// (e.g., /openapi/v1.json) with endpoints filtered to that version group.
    /// To add a new version, add another entry to the <c>OpenApiOptions:Versions</c> array
    /// or call <c>AddOpenApi("v2", ...)</c> after this method.
    /// </summary>
    public static IServiceCollection AddHeroOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<OpenApiOptions>()
            .Bind(configuration.GetSection(nameof(OpenApiOptions)))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Title), "OpenApi:Title is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Description), "OpenApi:Description is required.")
            .ValidateOnStart();

        var fshOptions = configuration.GetSection(nameof(OpenApiOptions)).Get<OpenApiOptions>();

        // Register a separate OpenAPI document per API version.
        // The GroupNameFormat "'v'VVV" in Asp.Versioning causes endpoints to be grouped as "v1", "v2", etc.
        // Each AddOpenApi(groupName) call creates a document that only includes endpoints from that group.
        var versions = fshOptions?.Versions is { Length: > 0 } ? fshOptions.Versions : ["v1"];
        foreach (var version in versions)
        {
            services.AddOpenApi(version, options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                options.AddDocumentTransformer((document, context, _) =>
                {
                    var provider = context.ApplicationServices;
                    var openApi = provider.GetRequiredService<IOptions<OpenApiOptions>>().Value;

                    document.Info = new OpenApiInfo
                    {
                        Title = openApi.Title,
                        Version = version,
                        Description = openApi.Description,
                        Contact = openApi.Contact is null ? null : new OpenApiContact
                        {
                            Name = openApi.Contact.Name,
                            Url = openApi.Contact.Url,
                            Email = openApi.Contact.Email
                        },
                        License = openApi.License is null ? null : new OpenApiLicense
                        {
                            Name = openApi.License.Name,
                            Url = openApi.License.Url
                        }
                    };
                    return Task.CompletedTask;
                });
            });
        }

        return services;
    }

    public static void UseHeroOpenApi(
        this WebApplication app,
        string openApiPath = "/openapi/{documentName}.json")
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapOpenApi(openApiPath);

        app.MapScalarApiReference(options =>
        {
            var configuration = app.Configuration;
            options
                .WithTitle(configuration["OpenApi:Title"] ?? "FSH API")
                .WithTheme(Scalar.AspNetCore.ScalarTheme.Alternate)
                .EnableDarkMode()
                .HideModels()
                .WithOpenApiRoutePattern(openApiPath)
                .AddPreferredSecuritySchemes("Bearer");
        });
    }
}
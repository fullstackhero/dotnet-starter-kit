using Microsoft.Extensions.DependencyInjection;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Factory for creating template services with dependency injection
/// </summary>
internal static class TemplateServices
{
    private static readonly Lazy<IServiceProvider> _serviceProvider = new(CreateServiceProvider);

    public static ITemplateRenderer GetRenderer() => _serviceProvider.Value.GetRequiredService<ITemplateRenderer>();

    public static ITemplateValidator GetValidator() => _serviceProvider.Value.GetRequiredService<ITemplateValidator>();

    public static ITemplateCache GetCache() => _serviceProvider.Value.GetRequiredService<ITemplateCache>();

    public static ITemplateLoader GetLoader() => _serviceProvider.Value.GetRequiredService<ITemplateLoader>();

    public static ITemplateParser GetParser() => _serviceProvider.Value.GetRequiredService<ITemplateParser>();

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // Register template services
        services.AddSingleton<ITemplateLoader, TemplateLoader>();
        services.AddSingleton<ITemplateParser, TemplateParser>();
        services.AddSingleton<ITemplateCache, TemplateCache>();
        services.AddScoped<ITemplateValidator, TemplateValidator>();
        services.AddScoped<ITemplateRenderer, TemplateRenderer>();

        return services.BuildServiceProvider();
    }
}
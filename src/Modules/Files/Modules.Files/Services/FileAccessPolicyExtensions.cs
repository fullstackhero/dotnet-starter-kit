using FSH.Modules.Files.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Files.Services;

/// <summary>
/// DI sugar for owning modules to register their <see cref="IFileAccessPolicy"/> implementations.
/// Usage in a module's <c>ConfigureServices</c>:
/// <c>builder.Services.AddFileAccessPolicy&lt;ProductImagePolicy&gt;();</c>
/// </summary>
public static class FileAccessPolicyExtensions
{
    public static IServiceCollection AddFileAccessPolicy<TPolicy>(this IServiceCollection services)
        where TPolicy : class, IFileAccessPolicy
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IFileAccessPolicy, TPolicy>();
        return services;
    }
}

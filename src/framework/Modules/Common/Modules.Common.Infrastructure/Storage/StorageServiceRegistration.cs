using FSH.Framework.Core.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Storage;
public static class StorageServiceRegistration
{
    public static IServiceCollection ConfigureLocalFileStorage(this IServiceCollection services)
    {
        // You can later use config["Storage:Provider"] to swap between implementations
        services.AddScoped<IStorageService, LocalStorageService>();
        return services;
    }
}
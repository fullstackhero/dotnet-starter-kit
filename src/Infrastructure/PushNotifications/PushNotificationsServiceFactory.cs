using FSH.WebApi.Application.Common.PushNotifications;
using FSH.WebApi.Infrastructure.Multitenancy;
using FSH.WebApi.Infrastructure.PushNotifications.OneSignal;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebApi.Infrastructure.PushNotifications;

public class PushNotificationsServiceFactory : IPushNotificationServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FSHTenantInfo _currentTenant;

    public PushNotificationsServiceFactory(IServiceProvider serviceProvider, FSHTenantInfo currentTenant)
    {
        _serviceProvider = serviceProvider;
        _currentTenant = currentTenant;
    }

    // Resolve the push notification service based on the current tenant's push notification provider if exists.
    public IPushNotificationsService? Create()
    {
        if (_currentTenant?.PushNotificationsSettings?.Provider is { } provider)
        {
            return provider switch
            {
                PushNotificationsProvider.OneSignal => _serviceProvider.GetRequiredService<OneSignalService>(),

                // PushNotificationProvider.Firebase => _serviceProvider.GetRequiredService<IFirebaseService>(),

                _ => throw new NotImplementedException($"Push notification service for {provider.GetType().Name} is not implemented.")
            };
        }

        return null;
    }
}
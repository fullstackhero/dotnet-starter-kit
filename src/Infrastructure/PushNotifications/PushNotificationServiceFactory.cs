using FSH.WebApi.Application.Common.PushNotifications;
using FSH.WebApi.Application.Multitenancy;
using FSH.WebApi.Infrastructure.Multitenancy;
using FSH.WebApi.Infrastructure.PushNotifications.OneSignal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Infrastructure.PushNotifications;

public class PushNotificationServiceFactory : IPushNotificationServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FSHTenantInfo _currentTenant;

    public PushNotificationServiceFactory(IServiceProvider serviceProvider, FSHTenantInfo currentTenant)
    {
        _serviceProvider = serviceProvider;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// Resolve the push notification service based on the current tenant's push notification provider.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">Thrown when the current tenant's push notification info is null.</exception>
    public IPushNotificationService Create()
    {
        ArgumentNullException.ThrowIfNull(_currentTenant.PushNotificationInfo, nameof(_currentTenant.PushNotificationInfo));

        return _currentTenant.PushNotificationInfo.Provider switch
        {
            PushNotificationProvider.OneSignal => _serviceProvider.GetRequiredService<OneSignalPushNotificationService>(),
            // PushNotificationProvider.Firebase => _serviceProvider.GetRequiredService<FirebasePushNotificationService>(),
            _ => throw new NotImplementedException()
        };
    }
}
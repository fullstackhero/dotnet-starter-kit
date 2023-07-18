using FSH.WebApi.Application.Common.PushNotifications;
using FSH.WebApi.Infrastructure.PushNotifications.OneSignal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Infrastructure.PushNotifications;

public static class Startup
{
    public static IServiceCollection AddPushNotifications(this IServiceCollection services)
    {
        //services.AddTransient<OneSignalPushNotificationService>();

        services.AddHttpClient(PushNotificationsConstants.HttpClientName, conf =>
        {
            // I could put basepath, authKey etc. here,
            // but push notification provider can vary accross tenants
            // and also authKey will be different for each tenant
        });

        return services;
    }
}
using FSH.WebApi.Application.Multitenancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Common.PushNotifications;

public interface IPushNotificationServiceFactory : ITransientService
{
    IPushNotificationsService? Create();
}
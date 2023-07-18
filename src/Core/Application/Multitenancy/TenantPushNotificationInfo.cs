using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Multitenancy;

public sealed record TenantPushNotificationInfo(PushNotificationProvider Provider, string AppId, string Name, string AuthKey, string IconUrl);

public enum PushNotificationProvider
{
    OneSignal = 1
}
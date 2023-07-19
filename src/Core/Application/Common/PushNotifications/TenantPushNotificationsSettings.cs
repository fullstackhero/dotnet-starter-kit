using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Common.PushNotifications;

public sealed record TenantPushNotificationsSettings(
    PushNotificationsProvider Provider,
    string AppId,
    string Name,
    string AuthKey,
    string IconUrl);
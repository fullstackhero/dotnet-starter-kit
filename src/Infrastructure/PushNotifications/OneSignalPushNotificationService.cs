using FSH.WebApi.Application.Common.PushNotifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Infrastructure.PushNotifications;

public class OneSignalPushNotificationService : IPushNotificationService
{
    public Task SendTo(string userId, PushNotificationType notificationType) => throw new NotImplementedException();

    public Task SendTo(ICollection<string> userIds, PushNotificationType notificationType) => throw new NotImplementedException();

    public Task SendToAll(PushNotificationType notificationType) => throw new NotImplementedException();

    //private string SendT
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Common.PushNotifications;

public interface IPushNotificationService : ITransientService
{
    Task SendTo(string userId, PushNotificationType notificationType);

    Task SendToAll(PushNotificationType notificationType);
}
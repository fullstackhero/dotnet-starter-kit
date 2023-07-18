using FSH.WebApi.Application.Common.PushNotifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Infrastructure.PushNotifications;

// Maybe make an api to accept more template?
public class PushNotificationTemplateFactory : IPushNotificationTemplateFactory
{
    private static readonly Dictionary<PushNotificationType, (string HeadingEN, string HeadingTR, string ContentEN, string ContentTR)> _notificationTypeToTemplateMap
        = new()
    {
        {
                PushNotificationType.ChargeCompletedNotification,
                ("Successfully charged.",
                "Şarj başarılı.",
                "You car has been charged successfully.",
                "Aracınız başarı ile şarj edildi.")
        },
        {
                PushNotificationType.ChargeStoppedWithErrorNotification,
                ("Charge failed.",
                "Şarj başarısız oldu.",
                "Charge operation is stopped due to error.",
                "Şarj işlemi bir hata sebebi ile durduruldu.")
        },

        // More types can be added here...
    };

    public (string HeadingEN, string HeadingTR, string ContentEN, string ContentTR) Create(PushNotificationType notificationType)
    {
        if (_notificationTypeToTemplateMap.TryGetValue(notificationType, out var template))
        {
            return template;
        }

        throw new NotImplementedException($"PushNotificationType {notificationType} is not implemented.");
    }
}
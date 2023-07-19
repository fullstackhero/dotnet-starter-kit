using FSH.WebApi.Application.Common.PushNotifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Identity.Users;
public sealed record SendPushNotificationsRequest
{
    public string UserId { get; }
    public string? PushNotificationsTemplateName { get; }
    public Message? CustomMessage { get; }
    public SendPushNotificationsRequest(string userId, string? pushNotificationsTemplateName, Message? customMessage)
    {
        if (pushNotificationsTemplateName is not null && customMessage is not null)
        {
            throw new ValidationException("Either PushNotificationsTemplateName or CustomMessage must be provided. You can not specify both at the same time.");
        }

        UserId = userId;
        PushNotificationsTemplateName = pushNotificationsTemplateName;
        CustomMessage = customMessage;
    }
}
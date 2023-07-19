using DocumentFormat.OpenXml.Spreadsheet;
using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Application.Common.Mailing;
using FSH.WebApi.Application.Identity.Users;
using FSH.WebApi.Application.Identity.Users.Password;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Infrastructure.Identity;

internal partial class UserService
{
    public async Task<string> SendPushNotificationsAsync(SendPushNotificationsRequest request, CancellationToken cancellationToken)
    {
        EnsureValidTenant();

        bool userExists = await _userManager.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException(_t["User ({0}) is not found.", request.UserId]);
        }

        if (_pushNotifications is null)
        {
            throw new ForbiddenException(_t["Your tenant's ({0}) push notifications settings has not been configured yet."]);
        }

        string returnMessage = string.Format(_t["Push notification sent to user {0}."], request.UserId);

        if (!string.IsNullOrWhiteSpace(request.PushNotificationsTemplateName))
        {
            await _pushNotifications.SendTo(request.UserId, request.PushNotificationsTemplateName);
            return returnMessage;
        }

        await _pushNotifications.SendTo(request.UserId, new[] { request.CustomMessage! });
        return returnMessage;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Infrastructure.PushNotifications.OneSignal;

public class OneSignalConstants
{
    public const string Url = "https://onesignal.com/api/v1/notifications";
    public const string AuthHeader = "Authorization";
    public const string AppId = "app_id";
    public const string AppName = "app_name";
    public const string Contents = "contents";
    public const string Headings = "headings";
    public const string LargeIcon = "large_icon";
    public const string IncludedSegments = "included_segments";
    public const string IncludeExternalUserIds = "include_external_user_ids";
    public const string AllUsers = "All Users";
    public const string ActiveUsers = "Active Users";
    public const string InactiveUsers = "Inactive Users";
}
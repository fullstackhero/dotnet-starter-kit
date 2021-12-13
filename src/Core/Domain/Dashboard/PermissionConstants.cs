using System.ComponentModel;

namespace DN.WebApi.Domain.Constants;

public partial class PermissionConstants
{
    [DisplayName("Dashboard")]
    [Description("Dashboard Permissions")]
    public static class Dashboard
    {
        public const string View = "Permissions.Dashboard.View";
    }
}
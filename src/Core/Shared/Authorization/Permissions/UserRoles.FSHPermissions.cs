using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Shared.Authorization.Permissions;
public static partial class FSHPermissions
{
    public static partial class UserRoles
    {
        public static readonly FSHPermission View = new("View User Roles", FSHAction.View, FSHResource.UserRoles);
        public static readonly FSHPermission Update = new("Update User Roles", FSHAction.Update, FSHResource.UserRoles);
    }
}

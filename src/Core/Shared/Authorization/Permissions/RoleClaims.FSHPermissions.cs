using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Shared.Authorization.Permissions;
public static partial class FSHPermissions
{
    public static partial class RoleClaims
    {
        public static readonly FSHPermission View = new("View Role Claims", FSHAction.View, FSHResource.RoleClaims);
        public static readonly FSHPermission Update = new("Update Role Claims", FSHAction.Update, FSHResource.RoleClaims);
    }
}

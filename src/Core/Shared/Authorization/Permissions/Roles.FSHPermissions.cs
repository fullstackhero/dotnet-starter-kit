using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Shared.Authorization.Permissions;
public static partial class FSHPermissions
{
    public static partial class Roles
    {
        public static readonly FSHPermission View = new("View Roles", FSHAction.View, FSHResource.Roles);
        public static readonly FSHPermission Create = new("Create Roles", FSHAction.Create, FSHResource.Roles);
        public static readonly FSHPermission Update = new("Update Roles", FSHAction.Update, FSHResource.Roles);
        public static readonly FSHPermission Delete = new("Delete Roles", FSHAction.Delete, FSHResource.Roles);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Shared.Authorization.Permissions;
public static partial class FSHPermissions
{
    public static partial class Users
    {
        public static readonly FSHPermission View = new("View Users", FSHAction.View, FSHResource.Users);
        public static readonly FSHPermission Search = new("Search Users", FSHAction.Search, FSHResource.Users);
        public static readonly FSHPermission Create = new("Create Users", FSHAction.Create, FSHResource.Users);
        public static readonly FSHPermission Update = new("Update Users", FSHAction.Update, FSHResource.Users);
        public static readonly FSHPermission Delete = new("Delete Users", FSHAction.Delete, FSHResource.Users);
        public static readonly FSHPermission Export = new("Export Users", FSHAction.Export, FSHResource.Users);
    }
}

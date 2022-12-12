using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Shared.Authorization.Permissions;
public static partial class FSHPermissions
{
    public static partial class Tenants
    {
        public static readonly FSHPermission View = new("View Tenants", FSHAction.View, FSHResource.Tenants, IsRoot: true);
        public static readonly FSHPermission Create = new("Create Tenants", FSHAction.Create, FSHResource.Tenants, IsRoot: true);
        public static readonly FSHPermission Update = new("Update Tenants", FSHAction.Update, FSHResource.Tenants, IsRoot: true);
        public static readonly FSHPermission UpgradeSubsription = new("Upgrade Tenant Subscription", FSHAction.UpgradeSubscription, FSHResource.Tenants, IsRoot: true);
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Shared.DTOs.Identity.Responses;

namespace DN.WebApi.Infrastructure.Extensions
{
    public static class ClaimsExtension
    {
        public static void GetAllPermissions(this List<RoleClaimResponse> allPermissions)
        {
            var modules = typeof(Permissions).GetNestedTypes();

            foreach (var module in modules)
            {
                string moduleName = string.Empty;
                string moduleDescription = string.Empty;

                if (module.GetCustomAttributes(typeof(DisplayNameAttribute), true)
                    .FirstOrDefault() is DisplayNameAttribute displayNameAttribute)
                {
                    moduleName = displayNameAttribute.DisplayName;
                }

                if (module.GetCustomAttributes(typeof(DescriptionAttribute), true)
                    .FirstOrDefault() is DescriptionAttribute descriptionAttribute)
                {
                    moduleDescription = descriptionAttribute.Description;
                }

                var fields = module.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                foreach (var fi in fields)
                {
                    object propertyValue = fi.GetValue(null);

                    if (propertyValue is not null)
                    {
                        allPermissions.Add(new RoleClaimResponse { Value = propertyValue.ToString(), Type = ClaimConstants.Permission, Group = moduleName, Description = moduleDescription });
                    }
                }
            }
        }
    }
}
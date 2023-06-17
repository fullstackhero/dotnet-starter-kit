using FL_CRMS_ERP_WEBAPI.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Auth.Permissions;

public class MustHavePermissionAttribute : AuthorizeAttribute
{
    public MustHavePermissionAttribute(string action, string resource) =>
        Policy = FSHPermission.NameFor(action, resource);
}
using Microsoft.AspNetCore.Authorization;

namespace DN.WebApi.Infrastructure.Auth.Permissions;

public class MustHavePermission : AuthorizeAttribute
{
    public MustHavePermission(string permission)
    {
        Policy = permission;
    }
}
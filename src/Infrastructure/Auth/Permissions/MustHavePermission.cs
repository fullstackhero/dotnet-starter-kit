using Microsoft.AspNetCore.Authorization;

namespace FSH.WebAPI.Infrastructure.Auth.Permissions;

public class MustHavePermission : AuthorizeAttribute
{
    public MustHavePermission(string permission)
    {
        Policy = permission;
    }
}
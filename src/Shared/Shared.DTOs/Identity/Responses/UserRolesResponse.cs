using System.Collections.Generic;

namespace DN.WebApi.Shared.DTOs.Identity.Responses
{
    public class UserRolesResponse
    {
        public List<UserRoleDto> UserRoles { get; set; } = new();
    }
}
using System.Collections.Generic;

namespace DN.WebApi.Shared.DTOs.Identity.Requests
{
    public class UserRolesRequest
    {
        public List<UserRoleDto> UserRoles { get; set; } = new();
    }
}
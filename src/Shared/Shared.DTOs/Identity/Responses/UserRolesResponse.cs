using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DN.WebApi.Shared.DTOs.Identity.Responses
{
    public class UserRolesResponse
    {
        public List<UserRoleDto> UserRoles { get; set; } = new();
    }
}
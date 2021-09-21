using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DN.WebApi.Shared.DTOs.Identity
{
    public class UserRoleDto
    {
        public string RoleId { get; set; }

        public string RoleName { get; set; }

        public bool Enabled { get; set; }
    }
}
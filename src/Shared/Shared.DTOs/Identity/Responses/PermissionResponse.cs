using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DN.WebApi.Shared.DTOs.Identity.Responses
{
    public class PermissionResponse
    {
        public string RoleId { get; set; }

        public string RoleName { get; set; }

        public List<RoleClaimResponse> RoleClaims { get; set; }
    }
}
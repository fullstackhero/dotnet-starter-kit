using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DN.WebApi.Shared.DTOs.Identity.Requests
{
    public class PermissionRequest
    {
        public string RoleId { get; set; }
        public IList<RoleClaimRequest> RoleClaims { get; set; }
    }
}
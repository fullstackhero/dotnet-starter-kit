using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DN.WebApi.Shared.DTOs.Identity.Requests
{
    public class UpdatePermissionsRequest
    {
        public string Permission { get; set; }
        public bool Enabled { get; set; }
    }
}
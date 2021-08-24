using DN.WebApi.Domain.Contracts;
using Microsoft.AspNetCore.Identity;

namespace DN.WebApi.Infrastructure.Identity.Models
{
    public class ExtendedRole : IdentityRole
    {
        public string Description { get; set; }

        public string TenantId { get; set; }
        public ExtendedRole() : base()
        {
        }
        public ExtendedRole(string roleName, string tenantId, string description = null) : base(roleName)
        {
            Description = description;
            NormalizedName = roleName.ToUpper();
            TenantId = tenantId;
        }
    }
}
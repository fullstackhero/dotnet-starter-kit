using DN.WebApi.Domain.Contracts;
using Microsoft.AspNetCore.Identity;

namespace DN.WebApi.Infrastructure.Identity.Models
{
    public class ApplicationRole : IdentityRole
    {
        public string Description { get; set; }

        public string TenantId { get; set; }

        public ApplicationRole()
        : base()
        {
        }

        public ApplicationRole(string roleName, string tenantId, string description = null)
        : base(roleName)
        {
            Description = description;
            NormalizedName = roleName.ToUpper();
            TenantId = tenantId;
        }
    }
}
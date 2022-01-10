using DN.WebApi.Domain.Multitenancy;
using Microsoft.AspNetCore.Identity;

namespace DN.WebApi.Infrastructure.Identity;

public class ApplicationRole : IdentityRole, IIdentityTenant
{
    public string? Description { get; set; }
    public string? Tenant { get; set; }

    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName, string? tenant, string? description = null)
    : base(roleName)
    {
        Description = description;
        NormalizedName = roleName.ToUpperInvariant();
        Tenant = tenant;
    }
}
using Microsoft.AspNetCore.Identity;

namespace FSH.WebApi.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }

    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName, string? description = null)
        : base(roleName)
    {
        Description = description;
        NormalizedName = roleName.ToUpperInvariant();
    }
}
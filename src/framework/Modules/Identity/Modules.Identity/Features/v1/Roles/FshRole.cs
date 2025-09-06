using Microsoft.AspNetCore.Identity;

namespace FSH.Framework.Identity.Infrastructure.Roles;
public class FshRole : IdentityRole
{
    public string? Description { get; set; }

    public FshRole(string name, string? description = null)
        : base(name)
    {
        Description = description;
        NormalizedName = name.ToUpperInvariant();
    }
}
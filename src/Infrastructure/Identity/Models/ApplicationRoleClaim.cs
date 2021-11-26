using Microsoft.AspNetCore.Identity;

namespace DN.WebApi.Infrastructure.Identity.Models;

public class ApplicationRoleClaim : IdentityRoleClaim<string>
{
    public string Description { get; set; }
    public string Tenant { get; set; }
    public string Group { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public virtual ApplicationRole Role { get; set; }

    public ApplicationRoleClaim()
    : base()
    {
    }

    public ApplicationRoleClaim(string roleClaimDescription = null, string roleClaimGroup = null)
    : base()
    {
        Description = roleClaimDescription;
        Group = roleClaimGroup;
    }
}
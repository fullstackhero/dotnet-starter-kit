using Microsoft.AspNetCore.Identity;

namespace FSH.WebApi.Infrastructure.Identity;

public class ApplicationRoleClaim : IdentityRoleClaim<string>
{
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }

    public ApplicationRoleClaim()
    {
    }

    public ApplicationRoleClaim(string? createdBy = null, DateTime? createdOn = null)
    {
        CreatedBy = createdBy;
        CreatedOn = createdOn ?? DateTime.UtcNow;
    }
}
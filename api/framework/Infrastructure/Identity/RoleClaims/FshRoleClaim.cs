using Microsoft.AspNetCore.Identity;

namespace FSH.Framework.Infrastructure.Identity.RoleClaims;
public class FshRoleClaim : IdentityRoleClaim<string>
{
    public string? CreatedBy { get; init; }
    public DateTime CreatedOn { get; init; }
}

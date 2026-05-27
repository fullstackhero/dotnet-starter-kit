using Microsoft.AspNetCore.Identity;

namespace FSH.Modules.Identity.Domain;

public class FshRoleClaim : IdentityRoleClaim<string>
{
    public string? CreatedBy { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
}
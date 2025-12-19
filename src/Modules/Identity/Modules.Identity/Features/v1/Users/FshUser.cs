using Microsoft.AspNetCore.Identity;

namespace FSH.Modules.Identity.Features.v1.Users;

public class FshUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Uri? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }

    public string? ObjectId { get; set; }

    /// <summary>
    /// The date and time when the password was last changed.
    /// Used to enforce password expiry policies.
    /// </summary>
    public DateTime? LastPasswordChangeUtc { get; set; }
}
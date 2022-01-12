using FSH.WebApi.Domain.Multitenancy;
using Microsoft.AspNetCore.Identity;

namespace FSH.WebApi.Infrastructure.Identity;

public class ApplicationUser : IdentityUser, IIdentityTenant
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    public string? Tenant { get; set; }

    public string? ObjectId { get; set; }
}
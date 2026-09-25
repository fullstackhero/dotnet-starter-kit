using System.Text.Json.Serialization;

namespace FSH.Modules.Identity.Contracts.DTOs;

public class UserDto
{
    public string? Id { get; set; }

    public string? UserName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailConfirmed { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Whether the user has enrolled in TOTP-based two-factor authentication.</summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// The stored optimistic-concurrency token for this user, populated only by the self-profile
    /// read. It never reaches the response body — <c>GET /identity/profile</c> turns it into the
    /// response's <c>ETag</c>, and that header is the token clients echo back in <c>If-Match</c>.
    /// </summary>
    [JsonIgnore]
    public string? ConcurrencyStamp { get; set; }
}
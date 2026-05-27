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
}
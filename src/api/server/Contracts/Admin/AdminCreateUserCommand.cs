using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.WebApi.Contracts.Admin;

public sealed class AdminCreateUserCommand
{
    [Required]
    public string Email { get; init; } = default!;

    [Required]
    public string Username { get; init; } = default!;

    [Required]
    public string PhoneNumber { get; init; } = default!;

    [Required]
    public string Tckn { get; init; } = default!;

    [Required]
    public string Password { get; init; } = default!;

    [Required]
    public string FirstName { get; init; } = default!;

    [Required]
    public string LastName { get; init; } = default!;

    public string? Profession { get; init; }

    [Required]
    public DateTime BirthDate { get; init; }

    public string? Status { get; init; }

    [Required]
    public bool IsIdentityVerified { get; init; }

    [Required]
    public bool IsPhoneVerified { get; init; }

    [Required]
    public bool IsEmailVerified { get; init; }
}

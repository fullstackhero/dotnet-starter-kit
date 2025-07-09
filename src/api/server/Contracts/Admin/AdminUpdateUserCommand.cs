using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.WebApi.Contracts.Admin;

public sealed class AdminUpdateUserCommand
{
    [Required]
    public string Email { get; init; } = default!;

    [Required]
    public string Username { get; init; } = default!;

    [Required]
    public string FirstName { get; init; } = default!;

    [Required]
    public string LastName { get; init; } = default!;

    [Required]
    public string PhoneNumber { get; init; } = default!;

    public int? ProfessionId { get; init; }

    [Required]
    public string Status { get; init; } = default!;

    [Required]
    public bool IsEmailVerified { get; init; }
}

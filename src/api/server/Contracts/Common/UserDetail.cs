using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.WebApi.Contracts.Common;

internal sealed class UserDetail
{
    [Required]
    public Guid Id { get; init; }

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

    public string? Profession { get; init; }

    [Required]
    public bool IsEmailVerified { get; init; }

    [Required]
    public bool EmailConfirmed { get; init; }

    [Required]
    public bool IsActive { get; init; }

    [Required]
    public IReadOnlyList<string> Roles { get; init; } = new List<string>();
}

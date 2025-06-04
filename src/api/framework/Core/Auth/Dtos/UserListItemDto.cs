namespace FSH.Framework.Core.Auth.Dtos;

public class UserListItemDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string PhoneNumber { get; init; } = string.Empty;
    public bool IsActive { get; init; }
} 
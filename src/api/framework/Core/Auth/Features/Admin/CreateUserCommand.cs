using MediatR;
using FSH.Framework.Core.Common.Models;
using FSH.Framework.Core.Auth.Domain.ValueObjects;

namespace FSH.Framework.Core.Auth.Features.Admin;

public record CreateUserCommand : IRequest<Result<CreateUserResult>>
{
    public Email Email { get; init; } = default!;
    public Username Username { get; init; } = default!;
    public PhoneNumber PhoneNumber { get; init; } = default!;
    public Tckn Tckn { get; init; } = default!;
    public Password Password { get; init; } = default!;
    public Name FirstName { get; init; } = default!;
    public Name LastName { get; init; } = default!;
    public Profession? Profession { get; init; }
    public BirthDate BirthDate { get; init; } = default!;
    public string? Status { get; init; }
    public bool IsIdentityVerified { get; init; }
    public bool IsPhoneVerified { get; init; }
    public bool IsEmailVerified { get; init; }
}

public record CreateUserResult
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string Message { get; init; } = default!;
} 
using System;
using MediatR;
using FSH.Framework.Core.Common.Models;

namespace FSH.Framework.Core.Auth.Features.Register;

public record RegisterCommand : IRequest<Result<RegisterResponseDto>>
{
    public string Email { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string PhoneNumber { get; init; } = default!;
    public string Tckn { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public int? ProfessionId { get; init; }
    public DateTime? BirthDate { get; init; }
}

public class RegisterResponseDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string? MemberNumber { get; init; }
    public string Message { get; init; } = default!;
}
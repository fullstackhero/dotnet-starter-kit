using MediatR;
using FSH.Framework.Core.Common.Models;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using System.Collections.ObjectModel;

namespace FSH.Framework.Core.Auth.Features.Login;

public record LoginCommand : IRequest<Result<LoginResponseDto>>
{
    public Tckn Tckn { get; init; } = default!;
    public Password Password { get; init; } = default!;
}

public class LoginResponseDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string AccessToken { get; init; } = default!;
    public string RefreshToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
    public ReadOnlyCollection<string> Roles { get; init; } = new List<string>().AsReadOnly();
} 
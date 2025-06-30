using System;
using MediatR;
using FSH.Framework.Core.Common.Models;

namespace FSH.Framework.Core.Auth.Features.PasswordReset;

public sealed record ValidateResetTokenCommand : IRequest<Result<ValidateResetTokenResponse>>
{
    public string Token { get; init; } = default!;
}

public sealed record ValidateResetTokenResponse
{
    public bool IsValid { get; init; }
    public string? Message { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
using System;
using MediatR;

namespace FSH.Framework.Core.Auth.Features.Admin;

public record DeleteUserCommand : IRequest<DeleteUserResult>
{
    public Guid UserId { get; init; }
}

public class DeleteUserResult
{
    public Guid UserId { get; init; }
    public string Message { get; init; } = default!;
}
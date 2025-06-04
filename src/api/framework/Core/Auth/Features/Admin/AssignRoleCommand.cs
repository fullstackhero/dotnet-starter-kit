using MediatR;

namespace FSH.Framework.Core.Auth.Features.Admin;

public record AssignRoleCommand : IRequest<AssignRoleResult>
{
    public Guid UserId { get; init; }
    public string Role { get; init; } = default!;
}

public class AssignRoleResult
{
    public Guid UserId { get; init; }
    public string Role { get; init; } = default!;
    public string Message { get; init; } = default!;
} 
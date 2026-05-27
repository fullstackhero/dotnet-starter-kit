using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Groups.UpdateGroup;

public sealed record UpdateGroupCommand(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault,
    IReadOnlyList<string>? RoleIds) : ICommand<GroupDto>;
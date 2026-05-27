using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Groups.CreateGroup;

public sealed record CreateGroupCommand(
    string Name,
    string? Description,
    bool IsDefault,
    List<string>? RoleIds) : ICommand<GroupDto>;
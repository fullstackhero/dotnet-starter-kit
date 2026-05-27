using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Roles.UpsertRole;

public class UpsertRoleCommand : ICommand<RoleDto>
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace FSH.Starter.WebApi.Contracts.Admin;

internal sealed class AssignRoleCommand
{
    [Required]
    public string RoleId { get; init; } = default!;
}

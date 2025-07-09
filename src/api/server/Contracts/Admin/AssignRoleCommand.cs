using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FSH.Starter.Tests.Unit")]

namespace FSH.Starter.WebApi.Contracts.Admin;

internal sealed class AssignRoleCommand
{
    [Required]
    public string RoleId { get; init; } = default!;
}

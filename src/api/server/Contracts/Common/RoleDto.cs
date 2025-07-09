using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FSH.Starter.Tests.Unit")]

namespace FSH.Starter.WebApi.Contracts.Common;

internal sealed class RoleDto
{
    [Required]
    public string Id { get; init; } = default!;

    [Required]
    public string Name { get; init; } = default!;

    [Required]
    public string Description { get; init; } = default!;
}

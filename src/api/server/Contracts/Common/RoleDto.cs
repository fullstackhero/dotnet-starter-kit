using System.ComponentModel.DataAnnotations;

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

namespace FSH.Modules.Identity.Contracts.DTOs;

public class GroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSystemGroup { get; set; }
    public int MemberCount { get; set; }
    public IReadOnlyCollection<string>? RoleIds { get; set; }
    public IReadOnlyCollection<string>? RoleNames { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
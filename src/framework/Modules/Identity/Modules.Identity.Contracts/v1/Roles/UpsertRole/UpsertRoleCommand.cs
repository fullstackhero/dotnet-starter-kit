namespace FSH.Framework.Identity.Endpoints.v1.Roles.CreateOrUpdateRole;

public class UpsertRoleCommand
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}
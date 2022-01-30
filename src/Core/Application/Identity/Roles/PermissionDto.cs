namespace FSH.WebApi.Application.Identity.Roles;

public class PermissionDto
{
    public string Permission { get; set; } = default!;
    public string? Group { get; set; }
    public string? Description { get; set; }
}
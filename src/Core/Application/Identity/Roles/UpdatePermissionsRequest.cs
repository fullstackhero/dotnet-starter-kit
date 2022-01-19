namespace FSH.WebApi.Application.Identity.Roles;

public class UpdatePermissionsRequest
{
    public List<string> Permissions { get; set; }
    public string RoleId { get; set; }
}
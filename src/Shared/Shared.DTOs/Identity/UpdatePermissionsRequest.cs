namespace DN.WebApi.Shared.DTOs.Identity;

public class UpdatePermissionsRequest
{
    public string Permission { get; set; }
    public bool Enabled { get; set; }
}
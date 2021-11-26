namespace DN.WebApi.Shared.DTOs.Multitenancy;

public class CreateTenantRequest
{
    public string Name { get; set; }
    public string Key { get; set; }
    public string AdminEmail { get; set; }
    public string ConnectionString { get; set; }
}
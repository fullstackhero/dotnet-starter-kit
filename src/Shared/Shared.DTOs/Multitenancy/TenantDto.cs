namespace DN.WebApi.Shared.DTOs.Multitenancy;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Key { get; set; }
    public string AdminEmail { get; set; }
    public string ConnectionString { get; set; }
    public bool IsActive { get; set; }
    public DateTime ValidUpto { get; set; }
}
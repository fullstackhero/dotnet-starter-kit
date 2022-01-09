namespace DN.WebApi.Domain.Multitenancy;

public interface IIdentityTenant
{
    public string? Tenant { get; set; }
}
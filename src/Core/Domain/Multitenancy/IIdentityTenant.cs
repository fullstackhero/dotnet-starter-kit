namespace FSH.WebAPI.Domain.Multitenancy;

public interface IIdentityTenant
{
    public string? Tenant { get; set; }
}
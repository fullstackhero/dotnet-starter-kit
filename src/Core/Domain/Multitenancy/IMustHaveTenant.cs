namespace DN.WebApi.Domain.Multitenancy;

public interface IMustHaveTenant
{
    public string? Tenant { get; set; }
}
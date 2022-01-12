namespace FSH.WebApi.Domain.Multitenancy;

public interface IMustHaveTenant
{
    public string? Tenant { get; set; }
}
namespace FSH.WebAPI.Domain.Multitenancy;

public interface IMustHaveTenant
{
    public string? Tenant { get; set; }
}
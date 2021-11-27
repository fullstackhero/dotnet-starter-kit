namespace DN.WebApi.Domain.Contracts;

public interface IMustHaveTenant
{
    public string Tenant { get; set; }
}
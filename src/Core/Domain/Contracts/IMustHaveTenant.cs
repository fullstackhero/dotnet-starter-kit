namespace DN.WebApi.Domain.Contracts
{
    public interface IMustHaveTenant
    {
        public string TenantKey { get; set; }
    }
}
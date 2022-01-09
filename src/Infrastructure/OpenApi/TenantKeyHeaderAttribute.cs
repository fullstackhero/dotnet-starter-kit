using DN.WebApi.Shared.Multitenancy;

namespace DN.WebApi.Infrastructure.OpenApi;

public class TenantKeyHeaderAttribute : SwaggerHeaderAttribute
{
    public TenantKeyHeaderAttribute()
        : base(
            MultitenancyConstants.TenantKeyName,
            "Input your tenant Id to access this API",
            string.Empty,
            true)
    {
    }
}

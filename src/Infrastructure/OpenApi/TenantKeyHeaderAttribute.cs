using FSH.WebApi.Shared.Multitenancy;

namespace FSH.WebApi.Infrastructure.OpenApi;

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

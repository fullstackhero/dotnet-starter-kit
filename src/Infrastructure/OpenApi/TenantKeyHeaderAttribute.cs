using FSH.WebAPI.Shared.Multitenancy;

namespace FSH.WebAPI.Infrastructure.OpenApi;

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

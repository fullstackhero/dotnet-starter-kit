using FL_CRMS_ERP_WEBAPI.Shared.Multitenancy;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.OpenApi;

public class TenantIdHeaderAttribute : SwaggerHeaderAttribute
{
    public TenantIdHeaderAttribute()
        : base(
            MultitenancyConstants.TenantIdName,
            "Input your tenant Id to access this API",
            string.Empty,
            true)
    {
    }
}

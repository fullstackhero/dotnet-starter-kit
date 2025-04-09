using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Tenant.Endpoints.v1.Activate;
public static partial class ActivateTenant
{
    public sealed record Command(string TenantId) : ICommand<Response>;
}

using FSH.Framework.Auditing.Contracts;

namespace FSH.Framework.Auditing.Endpoints.v1.GetUserTrails;
public static partial class GetUserTrails
{
    public sealed record Response(IReadOnlyList<Trail> AuditTrails);
}

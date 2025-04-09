using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Auditing.Endpoints.v1.GetUserTrails;
public static partial class GetUserTrails
{
    public sealed record Query(Guid UserId) : IQuery<Response>;
}

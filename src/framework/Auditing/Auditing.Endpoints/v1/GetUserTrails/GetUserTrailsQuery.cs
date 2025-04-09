using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Auditing.Endpoints.v1.GetUserTrails;
public sealed record GetUserTrailsQuery(Guid UserId) : IQuery<GetUserTrailsResponse>;

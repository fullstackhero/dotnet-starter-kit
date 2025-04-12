using FSH.Framework.Core.Messaging.CQRS;

namespace FSH.Framework.Auditing.Contracts.v1.GetUserTrails;
public sealed record GetUserTrailsQuery(Guid UserId) : IQuery<GetUserTrailsQueryResponse>;
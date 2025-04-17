using FSH.Framework.Auditing.Contracts.Dtos;

namespace FSH.Framework.Auditing.Contracts.v1.GetUserTrails;
public sealed record GetUserTrailsQueryResponse(IReadOnlyList<TrailDto> AuditTrails);
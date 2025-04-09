using FSH.Framework.Auditing.Contracts;

namespace FSH.Framework.Auditing.Endpoints.v1.GetUserTrails;
public sealed record GetUserTrailsResponse(IReadOnlyList<Trail> AuditTrails);

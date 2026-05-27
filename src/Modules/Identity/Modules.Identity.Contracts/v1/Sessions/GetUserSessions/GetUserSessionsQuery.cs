using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Sessions.GetUserSessions;

public sealed record GetUserSessionsQuery(Guid UserId) : IQuery<List<UserSessionDto>>;
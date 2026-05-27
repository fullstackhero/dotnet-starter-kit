using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Sessions.GetMySessions;

public sealed record GetMySessionsQuery : IQuery<List<UserSessionDto>>;
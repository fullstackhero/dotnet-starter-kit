using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.GetUser;

public sealed record GetUserQuery(string Id) : IQuery<UserDto>;
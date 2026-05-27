using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.GetUserProfile;

public sealed record GetCurrentUserProfileQuery(string UserId) : IQuery<UserDto>;
using System;
using MediatR;
using FSH.Framework.Core.Auth.Dtos;

namespace FSH.Framework.Core.Auth.Features.Profile;

public record GetUserProfileQuery : IRequest<UserDetailDto>
{
    public Guid UserId { get; init; }
}
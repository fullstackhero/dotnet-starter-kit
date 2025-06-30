using System;
using MediatR;

namespace FSH.Framework.Core.Auth.Features.Profile;

public class UpdateProfileCommand : IRequest<string>
{
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string? Profession { get; set; }
}
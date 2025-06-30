using System;
using MediatR;

namespace FSH.Framework.Core.Auth.Features.User;

public class UsernameExistsQuery : IRequest<bool>
{
    public string Username { get; }
    public Guid? ExcludeId { get; }

    public UsernameExistsQuery(string username, Guid? excludeId = null)
    {
        Username = username;
        ExcludeId = excludeId;
    }
}
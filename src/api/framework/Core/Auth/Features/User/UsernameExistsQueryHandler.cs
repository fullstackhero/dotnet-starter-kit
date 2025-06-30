using FSH.Framework.Core.Auth.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace FSH.Framework.Core.Auth.Features.User;

public class UsernameExistsQueryHandler : IRequestHandler<UsernameExistsQuery, bool>
{
    private readonly IUserRepository _users;

    public UsernameExistsQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<bool> Handle(UsernameExistsQuery request, CancellationToken cancellationToken)
    {
        return await _users.UsernameExistsAsync(request.Username, request.ExcludeId);
    }
}
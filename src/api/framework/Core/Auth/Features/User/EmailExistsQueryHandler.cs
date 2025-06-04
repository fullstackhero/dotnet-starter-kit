using FSH.Framework.Core.Auth.Repositories;
using MediatR;

namespace FSH.Framework.Core.Auth.Features.User;

public class EmailExistsQueryHandler : IRequestHandler<EmailExistsQuery, bool>
{
    private readonly IUserRepository _users;

    public EmailExistsQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<bool> Handle(EmailExistsQuery request, CancellationToken cancellationToken)
    {
        return await _users.EmailExistsAsync(request.Email, request.ExcludeId);
    }
} 
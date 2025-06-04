using MediatR;

namespace FSH.Framework.Core.Auth.Features.User;

public class EmailExistsQuery : IRequest<bool>
{
    public string Email { get; }
    public Guid? ExcludeId { get; }

    public EmailExistsQuery(string email, Guid? excludeId = null)
    {
        Email = email;
        ExcludeId = excludeId;
    }
} 
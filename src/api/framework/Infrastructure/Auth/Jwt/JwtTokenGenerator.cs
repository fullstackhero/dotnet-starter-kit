using FSH.Framework.Core.Auth.Services;
using FSH.Framework.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using FSH.Framework.Core.Auth.Jwt;

namespace FSH.Framework.Infrastructure.Auth.Jwt;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IOptions<JwtOptions> _options;
    public JwtTokenGenerator(IOptions<JwtOptions> options) => _options = options;

    public string GenerateToken(Guid id, string email, string username, string firstName, string lastName, string phoneNumber, string? profession, string status, IReadOnlyList<string> roles)
    {
        var user = new User
        {
            id = id,
            email = email,
            username = username,
            first_name = firstName,
            last_name = lastName,
            phone_number = phoneNumber,
            profession = profession,
            status = status
        };
        return JwtHelper.GenerateToken(user, _options.Value.Key, roles);
    }
} 
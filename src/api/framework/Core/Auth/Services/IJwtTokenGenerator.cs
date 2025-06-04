namespace FSH.Framework.Core.Auth.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid id, string email, string username, string firstName, string lastName, string phoneNumber, string? profession, string status, IReadOnlyList<string> roles);
} 
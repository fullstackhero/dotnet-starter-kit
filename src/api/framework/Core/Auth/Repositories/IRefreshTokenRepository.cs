namespace FSH.Framework.Core.Auth.Repositories;

public interface IRefreshTokenRepository
{
    Task SaveAsync(Guid userId, string refreshToken, DateTime expiryTime);
    Task<(bool IsValid, Guid UserId)> ValidateAsync(string refreshToken);
} 
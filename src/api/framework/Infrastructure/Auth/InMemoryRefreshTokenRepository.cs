using FSH.Framework.Core.Auth.Repositories;

namespace FSH.Framework.Infrastructure.Auth;

public class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly Dictionary<string, (Guid UserId, DateTime Expiry)> _store = new();

    public Task SaveAsync(Guid userId, string refreshToken, DateTime expiryTime)
    {
        _store[refreshToken] = (userId, expiryTime);
        return Task.CompletedTask;
    }

    public Task<(bool IsValid, Guid UserId)> ValidateAsync(string refreshToken)
    {
        if (_store.TryGetValue(refreshToken, out var data))
        {
            if (data.Expiry > DateTime.UtcNow)
            {
                return Task.FromResult((true, data.UserId));
            }
            // expired
            _store.Remove(refreshToken);
        }
        return Task.FromResult((false, Guid.Empty));
    }
} 
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using FSH.Framework.Core.Auth.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Infrastructure.Auth;

public sealed class PasswordResetService : IPasswordResetService
{
    private static readonly ConcurrentDictionary<string, string> _resetTokens = new();
    private static readonly ConcurrentDictionary<string, DateTime> _tokenExpiry = new();
    private static readonly ConcurrentDictionary<string, (int Count, DateTime FirstAttempt)> _rateLimitTracker = new();

    private readonly ILogger<PasswordResetService> _logger;
    private readonly DapperUserRepository _userRepository;
    private readonly int _tokenExpirationMinutes;
    private readonly int _maxAttemptsPerHour;

    public PasswordResetService(
        ILogger<PasswordResetService> logger, 
        IConfiguration configuration,
        DapperUserRepository userRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _tokenExpirationMinutes = configuration.GetValue<int>("PasswordReset:TokenExpirationMinutes", 15);
        _maxAttemptsPerHour = configuration.GetValue<int>("PasswordReset:MaxAttemptsPerHour", 3);
        
        _logger.LogDebug("PasswordResetService configured: TokenExpiration={TokenExpiration}min, MaxAttempts={MaxAttempts}/hour", 
            _tokenExpirationMinutes, _maxAttemptsPerHour);
    }

    public async Task<string> GenerateResetTokenAsync(string email)
    {
        try
        {
            // Clean up expired tokens
            CleanupExpiredTokens();
            
            // Check rate limiting
            if (await IsRateLimitedAsync(email))
            {
                throw new InvalidOperationException("Rate limit exceeded. Too many password reset attempts.");
            }

            var emailKey = email.ToUpperInvariant();
            var token = GenerateSecureToken();
            var expiryTime = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes);

            _resetTokens[emailKey] = token;
            _tokenExpiry[emailKey] = expiryTime;

            // Track this attempt for rate limiting
            await TrackResetAttemptAsync(email);

            _logger.LogInformation("Password reset token generated for email: {Email}, expires at: {ExpiryTime}", email, expiryTime);
            _logger.LogDebug("Password reset token for {Email}: {Token}", email, token);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate password reset token for email: {Email}", email);
            throw new InvalidOperationException($"Failed to generate password reset token for email: {email}", ex);
        }
    }

    public Task<bool> ValidateResetTokenAsync(string email, string token)
    {
        try
        {
            CleanupExpiredTokens();
            
            var emailKey = email.ToUpperInvariant();
            
            if (!_resetTokens.TryGetValue(emailKey, out var storedToken) ||
                !_tokenExpiry.TryGetValue(emailKey, out var expiryTime))
            {
                _logger.LogDebug("No token found for email: {Email}", email);
                return Task.FromResult(false);
            }

            if (DateTime.UtcNow > expiryTime)
            {
                _logger.LogDebug("Token expired for email: {Email} (expired at: {ExpiryTime})", email, expiryTime);
                return Task.FromResult(false);
            }

            var isValid = string.Equals(storedToken, token, StringComparison.Ordinal);
            _logger.LogDebug("Token validation for {Email}: {IsValid}", email, isValid);
            
            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password reset token for email: {Email}", email);
            return Task.FromResult(false);
        }
    }

    public Task InvalidateResetTokenAsync(string email)
    {
        try
        {
            var emailKey = email.ToUpperInvariant();
            
            _resetTokens.TryRemove(emailKey, out _);
            _tokenExpiry.TryRemove(emailKey, out _);
            
            _logger.LogDebug("Password reset token invalidated for email: {Email}", email);
            _logger.LogInformation("Password reset token invalidated for email: {Email}", email);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating password reset token for email: {Email}", email);
            throw new InvalidOperationException($"Error invalidating password reset token for email: {email}", ex);
        }
    }

    public Task<bool> IsRateLimitedAsync(string email)
    {
        try
        {
            var emailKey = email.ToUpperInvariant();
            var now = DateTime.UtcNow;
            
            if (_rateLimitTracker.TryGetValue(emailKey, out var attempts))
            {
                // Reset counter if more than an hour has passed
                if (now.Subtract(attempts.FirstAttempt).TotalHours >= 1)
                {
                    _rateLimitTracker.TryRemove(emailKey, out _);
                    return Task.FromResult(false);
                }
                
                if (attempts.Count >= _maxAttemptsPerHour)
                {
                    _logger.LogDebug("Rate limit exceeded for email: {Email} ({Count}/{Max})", email, attempts.Count, _maxAttemptsPerHour);
                    return Task.FromResult(true);
                }
            }
            
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for email: {Email}", email);
            throw new InvalidOperationException($"Error checking rate limit for email: {email}", ex);
        }
    }

    private static Task TrackResetAttemptAsync(string email)
    {
        var emailKey = email.ToUpperInvariant();
        var now = DateTime.UtcNow;
        
        _rateLimitTracker.AddOrUpdate(
            emailKey, 
            (1, now), 
            (key, existing) =>
            {
                // Reset if more than an hour has passed
                if (now.Subtract(existing.FirstAttempt).TotalHours >= 1)
                {
                    return (1, now);
                }

                return (existing.Count + 1, existing.FirstAttempt);
            });

        return Task.CompletedTask;
    }

    private static string GenerateSecureToken()
    {
        // Generate a cryptographically secure random token
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32]; // 256 bits
        rng.GetBytes(tokenBytes);
        
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Replace("/", string.Empty, StringComparison.Ordinal)
            .Replace("=", string.Empty, StringComparison.Ordinal);
            
        return token;
    }

    private static void CleanupExpiredTokens()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = new List<string>();

        foreach (var kvp in _tokenExpiry)
        {
            if (now > kvp.Value)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _resetTokens.TryRemove(key, out _);
            _tokenExpiry.TryRemove(key, out _);
        }
    }

    public async Task ResetUserPasswordAsync(string email, string newPassword)
    {
        try
        {
            _logger.LogInformation("Resetting password for email: {Email}", email);
            
            // Use Dapper repository to reset password by email
            await _userRepository.ResetPasswordAsync(email, newPassword);
            
            _logger.LogInformation("Password successfully reset for email: {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset password for email: {Email}", email);
            throw new InvalidOperationException($"Failed to reset password for email: {email}", ex);
        }
    }
} 
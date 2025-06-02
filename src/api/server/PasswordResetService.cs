using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Host;

public interface IPasswordResetService
{
    Task<string> GenerateResetTokenAsync(string email);
    Task<bool> ValidateResetTokenAsync(string email, string token);
    Task InvalidateResetTokenAsync(string email);
    Task<bool> IsRateLimitedAsync(string email);
}

public class PasswordResetService : IPasswordResetService
{
    private static readonly ConcurrentDictionary<string, string> _resetTokens = new ();
    private static readonly ConcurrentDictionary<string, DateTime> _tokenExpiry = new ();
    private static readonly ConcurrentDictionary<string, (int Count, DateTime FirstAttempt)> _rateLimitTracker = new ();

    private readonly ILogger<PasswordResetService> _logger;
    private readonly int _tokenExpirationMinutes;
    private readonly int _maxAttemptsPerHour;

    public PasswordResetService(ILogger<PasswordResetService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _tokenExpirationMinutes = configuration.GetValue<int>("PasswordReset:TokenExpirationMinutes", 15);
        _maxAttemptsPerHour = configuration.GetValue<int>("PasswordReset:MaxAttemptsPerHour", 3);
        
        Console.WriteLine($"[DEBUG] PasswordResetService configured: TokenExpiration={_tokenExpirationMinutes}min, MaxAttempts={_maxAttemptsPerHour}/hour");
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
            Console.WriteLine($"Password reset token for {email}: {token}");
            Console.WriteLine($"Token expires at: {expiryTime:yyyy-MM-dd HH:mm:ss} UTC");

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate password reset token for email: {Email}. Exception: {Message}", email, ex.Message);
            throw new InvalidOperationException($"Failed to generate password reset token for email: {email}", ex);
        }
    }

    public async Task<bool> ValidateResetTokenAsync(string email, string token)
    {
        try
        {
            CleanupExpiredTokens();
            
            var emailKey = email.ToUpperInvariant();
            
            if (!_resetTokens.TryGetValue(emailKey, out var storedToken) ||
                !_tokenExpiry.TryGetValue(emailKey, out var expiryTime))
            {
                Console.WriteLine($"[DEBUG] No token found for email: {email}");
                return false;
            }

            if (DateTime.UtcNow > expiryTime)
            {
                Console.WriteLine($"[DEBUG] Token expired for email: {email} (expired at: {expiryTime})");
                return false;
            }

            var isValid = string.Equals(storedToken, token, StringComparison.Ordinal);
            Console.WriteLine($"[DEBUG] Token validation for {email}: {isValid}");
            
            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password reset token for email: {Email}. Exception: {Message}", email, ex.Message);
            return false;
        }
    }

    public async Task InvalidateResetTokenAsync(string email)
    {
        try
        {
            var emailKey = email.ToUpperInvariant();
            
            _resetTokens.TryRemove(emailKey, out _);
            _tokenExpiry.TryRemove(emailKey, out _);
            
            Console.WriteLine($"[DEBUG] Password reset token invalidated for email: {email}");
            _logger.LogInformation("Password reset token invalidated for email: {Email}", email);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating password reset token for email: {Email}. Exception: {Message}", email, ex.Message);
            throw new InvalidOperationException($"Error invalidating password reset token for email: {email}", ex);
        }
    }

    public async Task<bool> IsRateLimitedAsync(string email)
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
                    return false;
                }
                
                if (attempts.Count >= _maxAttemptsPerHour)
                {
                    Console.WriteLine($"[DEBUG] Rate limit exceeded for email: {email} ({attempts.Count}/{_maxAttemptsPerHour})");
                    return true;
                }
            }
            
            return await Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for email: {Email}. Exception: {Message}", email, ex.Message);
            throw new InvalidOperationException($"Error checking rate limit for email: {email}", ex);
        }
    }

    private static async Task TrackResetAttemptAsync(string email)
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
            
        await Task.CompletedTask;
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
        var expiredKeys = new List<string>();
        
        foreach (var kvp in _tokenExpiry)
        {
            if (now > kvp.Value)
            {
                expiredKeys.Add(kvp.Key);
            }
        }
        
        foreach (var key in expiredKeys)
        {
            _resetTokens.TryRemove(key, out _);
            _tokenExpiry.TryRemove(key, out _);
        }
    }
} 

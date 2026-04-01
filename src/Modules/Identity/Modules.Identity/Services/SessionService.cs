using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UAParser;

namespace FSH.Modules.Identity.Services;

public sealed class SessionService(
    IdentityDbContext db,
    ICurrentUser currentUser,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    ILogger<SessionService> logger,
    TimeProvider timeProvider) : ISessionService
{
    private readonly Parser _uaParser = Parser.GetDefault();

    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new UnauthorizedAccessException("Invalid tenant");
        }
    }

    public async Task<UserSessionDto> CreateSessionAsync(
        string userId,
        string refreshTokenHash,
        string ipAddress,
        string userAgent,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var clientInfo = _uaParser.Parse(userAgent);

        var session = UserSession.Create(
            userId: userId,
            refreshTokenHash: refreshTokenHash,
            ipAddress: ipAddress,
            userAgent: userAgent,
            expiresAt: expiresAt,
            deviceType: DeviceTypeClassifier.Classify(clientInfo.Device.Family),
            browser: clientInfo.UA.Family,
            browserVersion: clientInfo.UA.Major,
            operatingSystem: clientInfo.OS.Family,
            osVersion: clientInfo.OS.Major);

        db.UserSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Created session {SessionId} for user {UserId}", session.Id, userId);
        }

        return MapToDto(session, isCurrentSession: true);
    }

    public async Task<List<UserSessionDto>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var currentUserId = currentUser.GetUserId().ToString();
        if (!string.Equals(userId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Cannot view sessions for another user");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var sessions = await db.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);

        return sessions.Select(s => MapToDto(s, isCurrentSession: false)).ToList();
    }

    public async Task<List<UserSessionDto>> GetUserSessionsForAdminAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var sessions = await db.UserSessions
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);

        return sessions.Select(s => MapToDto(s, isCurrentSession: false)).ToList();
    }

    public async Task<UserSessionDto?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await db.UserSessions
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        return session is null ? null : MapToDto(session, isCurrentSession: false);
    }

    public async Task<bool> RevokeSessionAsync(
        Guid sessionId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked, cancellationToken);

        if (session is null)
        {
            return false;
        }

        var currentUserId = currentUser.GetUserId().ToString();
        if (!string.Equals(session.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Cannot revoke session for another user");
        }

        var tenantId = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        session.Revoke(revokedBy, reason ?? "User requested", tenantId);

        await db.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Session {SessionId} revoked by {RevokedBy}", sessionId, revokedBy);
        }

        return true;
    }

    public async Task<int> RevokeAllSessionsAsync(
        string userId,
        string revokedBy,
        Guid? exceptSessionId = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var currentUserId = currentUser.GetUserId().ToString();
        if (!string.Equals(userId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Cannot revoke sessions for another user");
        }

        var query = db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked);

        if (exceptSessionId.HasValue)
        {
            query = query.Where(s => s.Id != exceptSessionId.Value);
        }

        var sessions = await query.ToListAsync(cancellationToken);

        var tenantId = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        foreach (var session in sessions)
        {
            session.Revoke(revokedBy, reason ?? "User requested logout from all devices", tenantId);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Revoked {Count} sessions for user {UserId}", sessions.Count, userId);
        }

        return sessions.Count;
    }

    public async Task<int> RevokeAllSessionsForAdminAsync(
        string userId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var sessions = await db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        var tenantId = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        foreach (var session in sessions)
        {
            session.Revoke(revokedBy, reason ?? "Admin requested", tenantId);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Admin {AdminId} revoked {Count} sessions for user {UserId}",
                revokedBy, sessions.Count, userId);
        }

        return sessions.Count;
    }

    public async Task<bool> RevokeSessionForAdminAsync(
        Guid sessionId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked, cancellationToken);

        if (session is null)
        {
            return false;
        }

        var tenantId = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        session.Revoke(revokedBy, reason ?? "Admin requested", tenantId);

        await db.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Admin {AdminId} revoked session {SessionId}", revokedBy, sessionId);
        }

        return true;
    }

    public async Task UpdateSessionActivityAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash && !s.IsRevoked, cancellationToken);

        if (session is not null)
        {
            session.UpdateActivity();
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateSessionRefreshTokenAsync(
        string oldRefreshTokenHash,
        string newRefreshTokenHash,
        DateTime newExpiresAt,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == oldRefreshTokenHash && !s.IsRevoked, cancellationToken);

        if (session is not null)
        {
            session.UpdateRefreshToken(newRefreshTokenHash, newExpiresAt);
            await db.SaveChangesAsync(cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Updated session {SessionId} with new refresh token", session.Id);
            }
        }
    }

    public async Task<bool> ValidateSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await db.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash, cancellationToken);

        if (session is null)
        {
            return true; // No session tracking for this token (backwards compatibility)
        }

        return !session.IsRevoked && session.ExpiresAt > timeProvider.GetUtcNow().UtcDateTime;
    }

    public async Task<Guid?> GetSessionIdByRefreshTokenAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await db.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash && !s.IsRevoked, cancellationToken);

        return session?.Id;
    }

    public async Task CleanupExpiredSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cutoffDate = now.AddDays(-30); // Keep revoked sessions for 30 days for audit
        var expiredSessions = await db.UserSessions
            .Where(s => s.ExpiresAt < now && s.ExpiresAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (expiredSessions.Count > 0)
        {
            db.UserSessions.RemoveRange(expiredSessions);
            await db.SaveChangesAsync(cancellationToken);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
            }
        }
    }

    private UserSessionDto MapToDto(UserSession session, bool isCurrentSession)
    {
        return new UserSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            UserName = session.User?.UserName,
            UserEmail = session.User?.Email,
            IpAddress = session.IpAddress,
            DeviceType = session.DeviceType,
            Browser = session.Browser,
            BrowserVersion = session.BrowserVersion,
            OperatingSystem = session.OperatingSystem,
            OsVersion = session.OsVersion,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            ExpiresAt = session.ExpiresAt,
            IsActive = !session.IsRevoked && session.ExpiresAt > timeProvider.GetUtcNow().UtcDateTime,
            IsCurrentSession = isCurrentSession
        };
    }
}
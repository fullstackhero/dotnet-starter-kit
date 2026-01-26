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

public sealed class SessionService : ISessionService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;
    private readonly ILogger<SessionService> _logger;
    private readonly Parser _uaParser;

    public SessionService(
        IdentityDbContext db,
        ICurrentUser currentUser,
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        ILogger<SessionService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _logger = logger;
        _uaParser = Parser.GetDefault();
    }

    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(_multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
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

        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created session {SessionId} for user {UserId}", session.Id, userId);

        return MapToDto(session, isCurrentSession: true);
    }

    public async Task<List<UserSessionDto>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var currentUserId = _currentUser.GetUserId().ToString();
        if (!string.Equals(userId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Cannot view sessions for another user");
        }

        var sessions = await _db.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);

        return sessions.Select(s => MapToDto(s, isCurrentSession: false)).ToList();
    }

    public async Task<List<UserSessionDto>> GetUserSessionsForAdminAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var sessions = await _db.UserSessions
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);

        return sessions.Select(s => MapToDto(s, isCurrentSession: false)).ToList();
    }

    public async Task<UserSessionDto?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await _db.UserSessions
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

        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked, cancellationToken);

        if (session is null)
        {
            return false;
        }

        var currentUserId = _currentUser.GetUserId().ToString();
        if (!string.Equals(session.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Cannot revoke session for another user");
        }

        var tenantId = _multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        session.Revoke(revokedBy, reason ?? "User requested", tenantId);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Session {SessionId} revoked by {RevokedBy}", sessionId, revokedBy);

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

        var currentUserId = _currentUser.GetUserId().ToString();
        if (!string.Equals(userId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Cannot revoke sessions for another user");
        }

        var query = _db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked);

        if (exceptSessionId.HasValue)
        {
            query = query.Where(s => s.Id != exceptSessionId.Value);
        }

        var sessions = await query.ToListAsync(cancellationToken);

        var tenantId = _multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        foreach (var session in sessions)
        {
            session.Revoke(revokedBy, reason ?? "User requested logout from all devices", tenantId);
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revoked {Count} sessions for user {UserId}", sessions.Count, userId);

        return sessions.Count;
    }

    public async Task<int> RevokeAllSessionsForAdminAsync(
        string userId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var sessions = await _db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        var tenantId = _multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        foreach (var session in sessions)
        {
            session.Revoke(revokedBy, reason ?? "Admin requested", tenantId);
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} revoked {Count} sessions for user {UserId}",
            revokedBy, sessions.Count, userId);

        return sessions.Count;
    }

    public async Task<bool> RevokeSessionForAdminAsync(
        Guid sessionId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked, cancellationToken);

        if (session is null)
        {
            return false;
        }

        var tenantId = _multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        session.Revoke(revokedBy, reason ?? "Admin requested", tenantId);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} revoked session {SessionId}", revokedBy, sessionId);

        return true;
    }

    public async Task UpdateSessionActivityAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash && !s.IsRevoked, cancellationToken);

        if (session is not null)
        {
            session.UpdateActivity();
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateSessionRefreshTokenAsync(
        string oldRefreshTokenHash,
        string newRefreshTokenHash,
        DateTime newExpiresAt,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == oldRefreshTokenHash && !s.IsRevoked, cancellationToken);

        if (session is not null)
        {
            session.UpdateRefreshToken(newRefreshTokenHash, newExpiresAt);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated session {SessionId} with new refresh token", session.Id);
        }
    }

    public async Task<bool> ValidateSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await _db.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash, cancellationToken);

        if (session is null)
        {
            return true; // No session tracking for this token (backwards compatibility)
        }

        return !session.IsRevoked && session.ExpiresAt > DateTime.UtcNow;
    }

    public async Task<Guid?> GetSessionIdByRefreshTokenAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();

        var session = await _db.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash && !s.IsRevoked, cancellationToken);

        return session?.Id;
    }

    public async Task CleanupExpiredSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep revoked sessions for 30 days for audit
        var expiredSessions = await _db.UserSessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow && s.ExpiresAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (expiredSessions.Count > 0)
        {
            _db.UserSessions.RemoveRange(expiredSessions);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
        }
    }

    private static UserSessionDto MapToDto(UserSession session, bool isCurrentSession)
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
            IsActive = !session.IsRevoked && session.ExpiresAt > DateTime.UtcNow,
            IsCurrentSession = isCurrentSession
        };
    }
}

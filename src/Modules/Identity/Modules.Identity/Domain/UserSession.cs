namespace FSH.Modules.Identity.Domain;

public class UserSession
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = default!;
    public string RefreshTokenHash { get; private set; } = default!;
    public string IpAddress { get; private set; } = default!;
    public string UserAgent { get; private set; } = default!;
    public string? DeviceType { get; private set; }
    public string? Browser { get; private set; }
    public string? BrowserVersion { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string? OsVersion { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedBy { get; private set; }
    public string? RevokedReason { get; private set; }

    // Navigation property
    public virtual FshUser? User { get; private set; }

    private UserSession() { } // EF Core

    public static UserSession Create(
        string userId,
        string refreshTokenHash,
        string ipAddress,
        string userAgent,
        DateTime expiresAt,
        string? deviceType = null,
        string? browser = null,
        string? browserVersion = null,
        string? operatingSystem = null,
        string? osVersion = null)
    {
        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = refreshTokenHash,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceType = deviceType,
            Browser = browser,
            BrowserVersion = browserVersion,
            OperatingSystem = operatingSystem,
            OsVersion = osVersion,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    public void UpdateRefreshToken(string refreshTokenHash, DateTime expiresAt)
    {
        RefreshTokenHash = refreshTokenHash;
        ExpiresAt = expiresAt;
        LastActivityAt = DateTime.UtcNow;
    }

    public void Revoke(string? revokedBy = null, string? reason = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        RevokedReason = reason;
    }
}

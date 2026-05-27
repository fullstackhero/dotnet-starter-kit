namespace FSH.Modules.Identity.Contracts.DTOs;

public class UserSessionDto
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OperatingSystem { get; set; }
    public string? OsVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentSession { get; set; }
}
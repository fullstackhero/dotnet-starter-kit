namespace FSH.Modules.Identity.Contracts.DTOs;

public sealed class PasswordExpiryStatusDto
{
    public bool IsExpired { get; set; }
    public bool IsExpiringWithinWarningPeriod { get; set; }
    public int DaysUntilExpiry { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string Status => this switch
    {
        { IsExpired: true } => "Expired",
        { IsExpiringWithinWarningPeriod: true } => "Expiring Soon",
        _ => "Valid"
    };
}
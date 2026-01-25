namespace FSH.Modules.Identity.Domain;

public class PasswordHistory
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public virtual FshUser? User { get; private set; }

    private PasswordHistory() { } // EF Core

    public static PasswordHistory Create(string userId, string passwordHash)
    {
        return new PasswordHistory
        {
            UserId = userId,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }
}

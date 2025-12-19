namespace FSH.Modules.Identity.Features.v1.Users;

public class UserPasswordHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public DateTime ChangedOnUtc { get; set; } = DateTime.UtcNow;
}

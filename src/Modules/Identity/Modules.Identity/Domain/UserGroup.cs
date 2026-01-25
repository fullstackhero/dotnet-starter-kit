namespace FSH.Modules.Identity.Domain;

public class UserGroup
{
    public string UserId { get; private set; } = default!;
    public Guid GroupId { get; private set; }
    public DateTime AddedAt { get; private set; }
    public string? AddedBy { get; private set; }

    // Navigation properties
    public virtual FshUser? User { get; private set; }
    public virtual Group? Group { get; private set; }

    private UserGroup() { } // EF Core

    public static UserGroup Create(string userId, Guid groupId, string? addedBy = null)
    {
        return new UserGroup
        {
            UserId = userId,
            GroupId = groupId,
            AddedAt = DateTime.UtcNow,
            AddedBy = addedBy
        };
    }
}

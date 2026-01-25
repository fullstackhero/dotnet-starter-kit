namespace FSH.Modules.Identity.Domain;

public class GroupRole
{
    public Guid GroupId { get; private set; }
    public string RoleId { get; private set; } = default!;

    // Navigation properties
    public virtual Group? Group { get; private set; }
    public virtual FshRole? Role { get; private set; }

    private GroupRole() { } // EF Core

    public static GroupRole Create(Guid groupId, string roleId)
    {
        return new GroupRole
        {
            GroupId = groupId,
            RoleId = roleId
        };
    }
}

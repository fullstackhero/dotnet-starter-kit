using FSH.Framework.Core.Domain;

namespace FSH.Modules.Identity.Domain;

public class Group : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsSystemGroup { get; private set; }

    // IAuditableEntity implementation
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    // ISoftDeletable implementation
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    // Navigation properties
    public virtual ICollection<GroupRole> GroupRoles { get; private set; } = [];
    public virtual ICollection<UserGroup> UserGroups { get; private set; } = [];

    private Group() { } // EF Core

    public static Group Create(string name, string? description = null, bool isDefault = false, bool isSystemGroup = false, string? createdBy = null)
    {
        return new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsDefault = isDefault,
            IsSystemGroup = isSystemGroup,
            CreatedAt = TimeProvider.System.GetUtcNow().UtcDateTime,
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string? description, string? modifiedBy = null)
    {
        Name = name;
        Description = description;
        ModifiedAt = TimeProvider.System.GetUtcNow().UtcDateTime;
        ModifiedBy = modifiedBy;
    }

    public void SetAsDefault(bool isDefault, string? modifiedBy = null)
    {
        IsDefault = isDefault;
        ModifiedAt = TimeProvider.System.GetUtcNow().UtcDateTime;
        ModifiedBy = modifiedBy;
    }
}

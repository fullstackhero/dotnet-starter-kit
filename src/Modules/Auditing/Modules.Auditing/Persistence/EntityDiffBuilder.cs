using FSH.Modules.Auditing.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FSH.Modules.Auditing.Persistence;

/// <summary>
/// Builds property-level diffs for EF Core entries. Skips navigations by default.
/// </summary>
internal static class EntityDiffBuilder
{
    private static readonly HashSet<Type> ScalarTypes =
    [
        typeof(string), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset),
        typeof(Guid), typeof(TimeSpan), typeof(byte[]), typeof(bool)
    ];

    internal sealed record Diff(
        string DbContext,
        string? Schema,
        string Table,
        string EntityName,
        string Key,
        EntityOperation Operation,
        IReadOnlyList<PropertyChange> Changes);

    public static List<Diff> Build(IEnumerable<EntityEntry> entries)
    {
        var list = new List<Diff>();

        foreach (var entry in entries)
        {
            var diff = BuildDiff(entry);
            if (diff is not null)
            {
                list.Add(diff);
            }
        }

        return list;
    }

    private static Diff? BuildDiff(EntityEntry entry)
    {
        var entityType = entry.Metadata;
        var table = entityType.GetTableName() ?? entityType.GetDefaultTableName() ?? entityType.DisplayName();
        var schema = entityType.GetSchema();
        var key = BuildKey(entry);
        var operation = DetermineOperation(entry);

        var changes = CollectPropertyChanges(entry);
        if (changes.Count == 0)
        {
            return null;
        }

        return new Diff(
            DbContext: entry.Context.GetType().Name,
            Schema: schema,
            Table: table!,
            EntityName: entityType.ClrType.Name,
            Key: key,
            Operation: operation,
            Changes: changes);
    }

    private static EntityOperation DetermineOperation(EntityEntry entry) => entry.State switch
    {
        EntityState.Added => EntityOperation.Insert,
        EntityState.Modified => DetectSoftDelete(entry) ? EntityOperation.SoftDelete : EntityOperation.Update,
        EntityState.Deleted => EntityOperation.Delete,
        _ => EntityOperation.None
    };

    private static List<PropertyChange> CollectPropertyChanges(EntityEntry entry)
    {
        var changes = new List<PropertyChange>();

        foreach (var property in entry.Properties)
        {
            if (ShouldSkipProperty(property))
            {
                continue;
            }

            var change = TryCreatePropertyChange(entry, property);
            if (change is not null)
            {
                changes.Add(change);
            }
        }

        return changes;
    }

    private static bool ShouldSkipProperty(PropertyEntry property)
    {
        var metadata = property.Metadata;

        if (metadata.IsShadowProperty() && !metadata.IsPrimaryKey()) return true;
        if (metadata.IsConcurrencyToken) return true;
        if (metadata.IsIndexerProperty()) return true;
        if (metadata.IsKey()) return true;
        if (!metadata.IsNullable && metadata.ClrType.IsClass && metadata.IsForeignKey()) return true;
        if (!IsScalar(metadata.ClrType)) return true;

        return false;
    }

    private static PropertyChange? TryCreatePropertyChange(EntityEntry entry, PropertyEntry property)
    {
        var (oldVal, newVal, isModified) = GetPropertyValues(entry.State, property);

        if (!isModified)
        {
            return null;
        }

        return new PropertyChange(
            Name: property.Metadata.Name,
            DataType: ToSimpleTypeName(property.Metadata.ClrType),
            OldValue: oldVal,
            NewValue: newVal,
            IsSensitive: IsSensitive(property.Metadata.Name));
    }

    private static (object? OldValue, object? NewValue, bool IsModified) GetPropertyValues(
        EntityState state,
        PropertyEntry property)
    {
        return state switch
        {
            EntityState.Added => (null, property.CurrentValue, true),
            EntityState.Modified => GetModifiedValues(property),
            EntityState.Deleted => (property.OriginalValue, null, true),
            _ => (null, null, false)
        };
    }

    private static (object? OldValue, object? NewValue, bool IsModified) GetModifiedValues(PropertyEntry property)
    {
        var oldVal = property.OriginalValue;
        var newVal = property.CurrentValue;
        var isModified = property.IsModified && !Equals(oldVal, newVal);

        return (oldVal, newVal, isModified);
    }

    private static string BuildKey(EntityEntry entry)
    {
        var keyProps = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToArray();
        if (keyProps.Length == 0)
        {
            return "<no-key>";
        }

        return string.Join("|", keyProps.Select(k => $"{k.Metadata.Name}:{k.CurrentValue ?? k.OriginalValue}"));
    }

    private static bool DetectSoftDelete(EntityEntry entry)
    {
        var prop = entry.Properties.FirstOrDefault(p =>
            p.Metadata.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase) &&
            p.Metadata.ClrType == typeof(bool));

        if (prop is null)
        {
            return false;
        }

        var orig = prop.OriginalValue as bool? ?? false;
        var curr = prop.CurrentValue as bool? ?? false;
        return !orig && curr;
    }

    private static bool IsSensitive(string propertyName) =>
        propertyName.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("token", StringComparison.OrdinalIgnoreCase);

    private static bool IsScalar(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t.IsPrimitive || t.IsEnum || ScalarTypes.Contains(t);
    }

    private static string ToSimpleTypeName(Type t) =>
        (Nullable.GetUnderlyingType(t) ?? t).Name;
}

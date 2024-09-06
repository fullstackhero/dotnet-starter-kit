using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Setting.Domain.Events;

namespace FSH.Starter.WebApi.Setting.Domain;
public class Dimension : AuditableEntity, IAggregateRoot
{
    public int Order { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public string? FullName { get; private set; }
    public string? NativeName { get; private set; }
    public string? FullNativeName { get; private set; }

    public int Value { get; private set; }
    public string Type { get; private set; }

    public Guid? FatherId { get; private set; }
    public virtual Dimension? Father { get; set; }
    public virtual ICollection<Dimension> InverseFather { get; private set; } = default!;

    public Dimension(
    int order,
    string code,
    string name,
    string? description,
    bool? isActive,
    string? fullname,
    string? nativeName,
    string? fullNativeName,
    int? value,
    string type,
    Guid? fatherId)
    {
        Order = order;
        Code = code;
        Name = name;
        Description = description;
        IsActive = isActive ?? true;
        FullName = fullname;
        NativeName = nativeName;
        FullNativeName = fullNativeName;
        Type = type;
        Value = value ?? 0;
        FatherId = fatherId == Guid.Empty ? null : fatherId;
    }

    public Dimension()
    : this(0, string.Empty, string.Empty, null, true, null, null, null, 0, string.Empty, null)
    {
    }

    public static Dimension Create(
    int? order,
    string code,
    string name,
    string? description,
    bool? isActive,
    string? fullname,
    string? nativeName,
    string? fullNativeName,
    int? value,
    string type,
    Guid? fatherId)
    {
        var item = new Dimension
        {
            Order = order ?? 0,
            Code = code,
            Name = name,
            Description = description,
            IsActive = isActive ?? true,
            FullName = fullname,
            NativeName = nativeName,
            FullNativeName = fullNativeName,
            Type = type,
            Value = value ?? 0,
            FatherId = fatherId == Guid.Empty ? null : fatherId,
        };

        item.QueueDomainEvent(new DimensionCreated(item.Id, item.Order, item.Code, item.Name, item.Description, item.IsActive, item.FullName, item.NativeName, item.FullNativeName, item.Value, item.Type, item.FatherId));

        DimensionMetrics.Created.Add(1);

        return item;
    }


    public Dimension Update(
        int? order,
        string? code,
        string? name,
        string? description,
        bool? isActive,
        string? fullName,
        string? nativeName,
        string? fullNativeName,
        int? value,
        string? type,
        Guid? fatherId)
    {
        if (order.HasValue && Order != order) Order = order.Value;
        if (code is not null && Code?.Equals(code, StringComparison.Ordinal) is not true) Code = code;
        if (name is not null && Name?.Equals(name, StringComparison.Ordinal) is not true) Name = name;
        if (description is not null && Description?.Equals(description, StringComparison.OrdinalIgnoreCase) is not true) Description = description;

        if (isActive is not null && !IsActive.Equals(isActive)) IsActive = (bool)isActive;

        if (fullName is not null && FullName?.Equals(fullName, StringComparison.OrdinalIgnoreCase) is not true) FullName = fullName;
        if (nativeName is not null && NativeName?.Equals(nativeName, StringComparison.OrdinalIgnoreCase) is not true) NativeName = nativeName;
        if (fullNativeName is not null && FullNativeName?.Equals(fullNativeName, StringComparison.OrdinalIgnoreCase) is not true) FullNativeName = fullNativeName;

        if (value.HasValue && Value != value) Value = value.Value;
        if (type is not null && Type?.Equals(type, StringComparison.Ordinal) is not true) Type = type;

        if (fatherId == Guid.Empty || fatherId == null)
        {
            FatherId = null;
        }
        else if (!FatherId.Equals(fatherId.Value))
        {
            FatherId = fatherId.Value;
        }

        QueueDomainEvent(new DimensionUpdated(this));

        return this;
    }

    public static Dimension Update(
        Guid id,
        int? order,
        string code,
        string name,
        string? description,
        bool? isActive,
        string? fullname,
        string? nativeName,
        string? fullNativeName,
        int? value,
        string type,
        Guid? fatherId)
    {
        var item = new Dimension
        {
            Id = id,
            Order = order ?? 0,
            Code = code,
            Name = name,
            Description = description,
            IsActive = isActive ?? true,
            FullName = fullname,
            NativeName = nativeName,
            FullNativeName = fullNativeName,
            Type = type,
            Value = value ?? 0,
            FatherId = fatherId == Guid.Empty ? null : fatherId,
        };

        item.QueueDomainEvent(new DimensionUpdated(item));

        return item;
    }

}

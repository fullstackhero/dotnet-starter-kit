using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Setting.Domain.Events;

namespace FSH.Starter.WebApi.Setting.Domain;
public class EntityCode : AuditableEntity, IAggregateRoot
{
    public int Order { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public string Separator { get; private set; }
    public int? Value { get; private set; }
    public CodeType Type { get; private set; }

    private EntityCode(
    int order,
    string code,
    string name,
    string? description,
    bool? isActive,
    string separator,
    int? value,
    CodeType? type)
    {
        Order = order;
        Code = code;
        Name = name;
        Description = description;
        IsActive = isActive ?? true;
        Separator = separator;
        Value = value ?? 0;
        Type = type ?? CodeType.MasterData;
    }

    private EntityCode()
    : this(0, string.Empty, string.Empty, null, true, string.Empty, 0, CodeType.MasterData)
    {
    }

    public static EntityCode Create(
    int? order,
    string code,
    string name,
    string? description,
    bool? isActive,
    string? separator,
    int? value,
    CodeType? type)
    {
        var item = new EntityCode
        {
            Order = order ?? 0,
            Code = code,
            Name = name,
            Description = description,
            IsActive = isActive ?? true,
            Separator = separator ?? string.Empty,
            Value = value ?? 0,
            Type = type ?? CodeType.MasterData
        };

        item.QueueDomainEvent(new EntityCodeCreated(item.Id, item.Order, item.Code, item.Name, item.Description, item.IsActive, item.Separator, item.Value, item.Type));

        EntityCodeMetrics.Created.Add(1);

        return item;
    }


    public EntityCode Update(
        int? order,
        string? code,
        string? name,
        string? description,
        bool? isActive,
        string? separator,
        int? value,
        CodeType? type)
    {
        if (order.HasValue && Order != order) Order = order.Value;
        if (code is not null && Code?.Equals(code, StringComparison.Ordinal) is not true) Code = code;
        if (name is not null && Name?.Equals(name, StringComparison.Ordinal) is not true) Name = name;
        if (description is not null && Description?.Equals(description, StringComparison.OrdinalIgnoreCase) is not true) Description = description;
        if (isActive is not null && !IsActive.Equals(isActive)) IsActive = (bool)isActive;
        
        if (separator is not null && Separator?.Equals(separator, StringComparison.Ordinal) is not true) Separator = separator;
        if (value is not null && Value != value) Value = value.Value;
        if (type is not null && !Type.Equals(CodeType.All) && !Type.Equals(type)) Type = type.Value;

        QueueDomainEvent(new EntityCodeUpdated(this));

        return this;
    }

    public static EntityCode Update(
        Guid id,
        int? order,
        string code,
        string name,
        string? description,
        bool? isActive,
        string? separator,
        int? value,
        CodeType? type)
    {
        var item = new EntityCode
        {
            Id = id,
            Order = order ?? 0,
            Code = code,
            Name = name,
            Description = description,
            IsActive = isActive ?? true,
            Separator = separator ?? string.Empty,
            Value = value ?? 0,
            Type = type?? CodeType.MasterData
        };

        item.QueueDomainEvent(new EntityCodeUpdated(item));

        return item;
    }
    
    
    public string AutoCode => GenerateCode();

    private string GenerateCode()
    {
        return Type switch
        {
            CodeType.MasterData => Name + Separator + DateTime.UtcNow.ToString("yyyyMMdd_HHmm"),
            CodeType.Transaction => Name + Separator + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"),
            CodeType.FastTransaction => Name + Separator + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"),

            _ => Guid.NewGuid().ToString()
        };
    }

}

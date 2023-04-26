namespace FSH.WebApi.Domain.Geo;
public class GeoAdminUnit : AuditableEntity, IAggregateRoot
{
    public int Order { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; set; }

    public string? FullName { get; private set; }
    public string? NativeName { get; private set; }
    public string? FullNativeName { get; private set; }

    public int Grade { get; private set; }
    public GeoAdminUnitType Type { get; private set; }

    public GeoAdminUnit(string code, string name, string? fullName, string? navetiveName, string? fullNavetiveName, string? description, int grade, GeoAdminUnitType type, int order, bool isActive)
    {
        Order = order;
        Code = code;
        Name = name;
        Description = description ?? string.Empty;
        IsActive = isActive;

        FullName = fullName;
        NativeName = navetiveName;
        FullNativeName = fullNavetiveName;

        Grade = grade;
        Type = type;
    }

    public GeoAdminUnit()
    : this(string.Empty, string.Empty, null, null, null, null, 0, GeoAdminUnitType.Ward, 0, true)
    {
    }

    public GeoAdminUnit Update(
        string? code,
        string? name,
        string? fullName,
        string? nativeName,
        string? fullNativeName,
        string? description,
        int? grade,
        GeoAdminUnitType? type,
        int? order,
        bool? isActive)
    {
        if (order is not null && order.HasValue && Order != order) Order = order.Value;
        if (code is not null && Code?.Equals(code) is not true) Code = code;
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (isActive is not null && !IsActive.Equals(isActive)) IsActive = (bool)isActive;

        if (fullName is not null && FullName?.Equals(fullName) is not true) FullName = fullName;
        if (nativeName is not null && NativeName?.Equals(nativeName) is not true) NativeName = nativeName;
        if (fullNativeName is not null && FullNativeName?.Equals(fullNativeName) is not true) FullNativeName = fullNativeName;

        if (grade is not null && grade.HasValue && Grade != grade) Grade = grade.Value;
        if (type is not null && !Type.Equals(type)) Type = type.Value;

        return this;
    }
}

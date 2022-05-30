namespace FSH.WebApi.Domain.Dog;
public class DogColor : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public bool? IsStandard { get; private set; }
    public string? RegistrationCode { get; private set; }

    public DogColor() { }
    public DogColor(string name, bool? isstandard, string? registrationcode)
    {
        Name = name;
        IsStandard = isstandard;
        RegistrationCode = registrationcode;
    }
    public DogColor Update(string name, bool? isstandard, string? registrationcode)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (IsStandard?.Equals(isstandard) is not true) IsStandard = isstandard;
        if (registrationcode is not null && RegistrationCode?.Equals(registrationcode) is not true) RegistrationCode = registrationcode;

        return this;
    }
}

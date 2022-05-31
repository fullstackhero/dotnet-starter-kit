namespace FSH.WebApi.Domain.Dog;
public class DogColor : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public bool? IsStandard { get; set; }
    public string? RegistrationCode { get; set; }
    public List<DogBreed>? Breeds { get; set; }
    public List<Dog>? Dogs { get; set; }

    public DogColor Update(string name, bool? isstandard, string? registrationcode)
    {
        if (name is not null && Name?.Equals(name) is not true) Name = name;
        if (IsStandard?.Equals(isstandard) is not true) IsStandard = isstandard;
        if (registrationcode is not null && RegistrationCode?.Equals(registrationcode) is not true) RegistrationCode = registrationcode;

        return this;
    }
}

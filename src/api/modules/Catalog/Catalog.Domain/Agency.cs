using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Catalog.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain;
public class Agency : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Telephone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Agency() { }

    private Agency(Guid id, string name, string email, string telephone, string address)
    {
        Id = id;
        Name = name;
        Email = email;
        Telephone = telephone;
        Address = address;
        QueueDomainEvent(new AgencyCreated { Agency = this });
    }

    public static Agency Create(string name, string email, string telephone, string address)
    {
        return new Agency(Guid.NewGuid(), name, email, telephone, address);
    }

    public Agency Update(string? name, string? email, string? telephone, string? address)
    {
        bool isUpdated = false;

        if (!string.IsNullOrWhiteSpace(name) && !string.Equals(Name, name, StringComparison.OrdinalIgnoreCase))
        {
            Name = name;
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(email) && !string.Equals(Email, email, StringComparison.OrdinalIgnoreCase))
        {
            Email = email;
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(telephone) && !string.Equals(Telephone, telephone, StringComparison.OrdinalIgnoreCase))
        {
            Telephone = telephone;
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(address) && !string.Equals(Address, address, StringComparison.OrdinalIgnoreCase))
        {
            Address = address;
            isUpdated = true;
        }

        if (isUpdated)
        {
            QueueDomainEvent(new AgencyUpdated { Agency = this });
        }

        return this;
    }
}

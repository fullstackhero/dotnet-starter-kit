using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Water.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain;

public class Customer : AuditableEntity, IAggregateRoot
{
    public string CustomerCode { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? ContactNumber { get; private set; }
    public string? Email { get; private set; }
    public ConnectionType ConnectionType { get; private set; }
    public CustomerStatus Status { get; private set; }

    private Customer() { }

    private Customer(Guid id, string customerCode, string fullName, string? address, string? contactNumber, string? email, ConnectionType connectionType)
    {
        Id = id;
        CustomerCode = customerCode;
        FullName = fullName;
        Address = address;
        ContactNumber = contactNumber;
        Email = email;
        ConnectionType = connectionType;
        Status = CustomerStatus.Active;

        QueueDomainEvent(new CustomerCreated { Customer = this });
    }

    public static Customer Create(string customerCode, string fullName, string? address, string? contactNumber, string? email, ConnectionType connectionType)
    {
        return new Customer(Guid.NewGuid(), customerCode, fullName, address, contactNumber, email, connectionType);
    }

    public Customer Update(string? fullName, string? address, string? contactNumber, string? email, ConnectionType? connectionType, CustomerStatus? status)
    {
        bool isUpdated = false;

        if (!string.IsNullOrWhiteSpace(fullName) && !string.Equals(FullName, fullName, StringComparison.OrdinalIgnoreCase))
        {
            FullName = fullName;
            isUpdated = true;
        }

        if (!string.Equals(Address, address, StringComparison.OrdinalIgnoreCase))
        {
            Address = address;
            isUpdated = true;
        }

        if (!string.Equals(ContactNumber, contactNumber, StringComparison.OrdinalIgnoreCase))
        {
            ContactNumber = contactNumber;
            isUpdated = true;
        }

        if (!string.Equals(Email, email, StringComparison.OrdinalIgnoreCase))
        {
            Email = email;
            isUpdated = true;
        }

        if (connectionType.HasValue && ConnectionType != connectionType.Value)
        {
            ConnectionType = connectionType.Value;
            isUpdated = true;
        }

        if (status.HasValue && Status != status.Value)
        {
            Status = status.Value;
            isUpdated = true;
        }

        if (isUpdated)
        {
            QueueDomainEvent(new CustomerUpdated { Customer = this });
        }

        return this;
    }
}

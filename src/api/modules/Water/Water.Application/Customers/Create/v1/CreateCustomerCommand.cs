using System.ComponentModel;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Customers.Create.v1;

public sealed record CreateCustomerCommand(
    [property: DefaultValue("WTR-0001")] string CustomerCode,
    [property: DefaultValue("John Doe")] string FullName,
    [property: DefaultValue("123 Main St")] string? Address = null,
    [property: DefaultValue("+1234567890")] string? ContactNumber = null,
    [property: DefaultValue("john@example.com")] string? Email = null,
    [property: DefaultValue(ConnectionType.Residential)] ConnectionType ConnectionType = ConnectionType.Residential) : IRequest<CreateCustomerResponse>;

using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Customers.Update.v1;

public sealed record UpdateCustomerCommand(
    Guid Id,
    string? FullName,
    string? Address = null,
    string? ContactNumber = null,
    string? Email = null,
    ConnectionType? ConnectionType = null,
    CustomerStatus? Status = null) : IRequest<UpdateCustomerResponse>;

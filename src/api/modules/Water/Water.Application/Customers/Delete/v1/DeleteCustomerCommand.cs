using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Customers.Delete.v1;

public sealed record DeleteCustomerCommand(Guid Id) : IRequest;

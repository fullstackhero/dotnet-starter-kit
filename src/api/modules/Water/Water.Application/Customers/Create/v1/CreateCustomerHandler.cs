using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Customers.Create.v1;

public sealed class CreateCustomerHandler(
    ILogger<CreateCustomerHandler> logger,
    [FromKeyedServices("water:customers")] IRepository<Customer> repository)
    : IRequestHandler<CreateCustomerCommand, CreateCustomerResponse>
{
    public async Task<CreateCustomerResponse> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var customer = Customer.Create(request.CustomerCode, request.FullName, request.Address, request.ContactNumber, request.Email, request.ConnectionType);
        await repository.AddAsync(customer, cancellationToken);
        logger.LogInformation("customer created {CustomerId}", customer.Id);
        return new CreateCustomerResponse(customer.Id);
    }
}

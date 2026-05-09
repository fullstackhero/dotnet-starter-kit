using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Customers.Update.v1;

public sealed class UpdateCustomerHandler(
    ILogger<UpdateCustomerHandler> logger,
    [FromKeyedServices("water:customers")] IRepository<Customer> repository)
    : IRequestHandler<UpdateCustomerCommand, UpdateCustomerResponse>
{
    public async Task<UpdateCustomerResponse> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var customer = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = customer ?? throw new CustomerNotFoundException(request.Id);
        var updatedCustomer = customer.Update(request.FullName, request.Address, request.ContactNumber, request.Email, request.ConnectionType, request.Status);
        await repository.UpdateAsync(updatedCustomer, cancellationToken);
        logger.LogInformation("customer with id : {CustomerId} updated.", customer.Id);
        return new UpdateCustomerResponse(customer.Id);
    }
}

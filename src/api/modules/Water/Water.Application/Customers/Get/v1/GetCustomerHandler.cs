using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.Customers.Get.v1;

public sealed class GetCustomerHandler(
    [FromKeyedServices("water:customers")] IReadRepository<Customer> repository,
    ICacheService cache)
    : IRequestHandler<GetCustomerRequest, CustomerResponse>
{
    public async Task<CustomerResponse> Handle(GetCustomerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"customer:{request.Id}",
            async () =>
            {
                var customer = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (customer == null) throw new CustomerNotFoundException(request.Id);
                return new CustomerResponse(customer.Id, customer.CustomerCode, customer.FullName, customer.Address, customer.ContactNumber, customer.Email, customer.ConnectionType, customer.Status);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}

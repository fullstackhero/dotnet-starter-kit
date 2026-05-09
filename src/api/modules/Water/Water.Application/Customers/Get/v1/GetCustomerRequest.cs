using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Customers.Get.v1;

public class GetCustomerRequest : IRequest<CustomerResponse>
{
    public Guid Id { get; set; }
    public GetCustomerRequest(Guid id) => Id = id;
}

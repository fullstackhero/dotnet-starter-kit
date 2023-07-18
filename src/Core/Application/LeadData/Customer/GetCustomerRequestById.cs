using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Customer;
public class GetCustomerRequestById : IRequest<CustomerDto>
{
    public Guid Id { get; set; }

    public GetCustomerRequestById(Guid id) => Id = id;
}

public class CustomerByIdSpec : Specification<CustomerDetailsModel, CustomerDto>, ISingleResultSpecification
{
    public CustomerByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetCustomerRequestHandler : IRequestHandler<GetCustomerRequestById, CustomerDto>
{
    private readonly IRepository<CustomerDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public GetCustomerRequestHandler(IRepository<CustomerDetailsModel> repository, IStringLocalizer<GetCustomerRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<CustomerDto> Handle(GetCustomerRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<CustomerDetailsModel, CustomerDto>)new CustomerByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Customer {0} Not Found.", request.Id]);
}

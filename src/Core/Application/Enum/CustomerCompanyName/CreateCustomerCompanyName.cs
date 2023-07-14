using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerCompanyName;

public class CreateCustomerCompanyName : IRequest<DefaultIdType>
{
    public string? CompanyName { get; set; }
}

public class CreateCustomerCompanyNameHandler : IRequestHandler<CreateCustomerCompanyName, DefaultIdType>
{
    private readonly IRepositoryWithEvents<CustomerCompanyNameModel> _repository;

    public CreateCustomerCompanyNameHandler(IRepositoryWithEvents<CustomerCompanyNameModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateCustomerCompanyName request, CancellationToken cancellationToken)
    {
        var invoice = new CustomerCompanyNameModel(request.CompanyName);

        await _repository.AddAsync(invoice, cancellationToken);

        return invoice.Id;
    }
}

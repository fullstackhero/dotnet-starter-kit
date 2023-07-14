using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerPolicyStatus;

public class CreateCustomerPolicyStatus : IRequest<DefaultIdType>
{
    public string? ProductName { get; set; }
}

public class CreateCustomerPolicyStatusHandler : IRequestHandler<CreateCustomerPolicyStatus, DefaultIdType>
{
    private readonly IRepositoryWithEvents<CustomerPolicyStatusModel> _repository;

    public CreateCustomerPolicyStatusHandler(IRepositoryWithEvents<CustomerPolicyStatusModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateCustomerPolicyStatus request, CancellationToken cancellationToken)
    {
        var returnValue = new CustomerPolicyStatusModel(request.ProductName);

        await _repository.AddAsync(returnValue, cancellationToken);

        return returnValue.Id;
    }
}

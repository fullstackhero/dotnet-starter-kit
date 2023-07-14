using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerModeOfPayment;

public class CreateCustomerModeOfPayment : IRequest<DefaultIdType>
{
    public string? PaymentType { get; set; }
}

public class CreateCustomerModeOfPaymentHandler : IRequestHandler<CreateCustomerModeOfPayment, DefaultIdType>
{
    private readonly IRepositoryWithEvents<CustomerModeOfPaymentModel> _repository;

    public CreateCustomerModeOfPaymentHandler(IRepositoryWithEvents<CustomerModeOfPaymentModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateCustomerModeOfPayment request, CancellationToken cancellationToken)
    {
        var returnValue = new CustomerModeOfPaymentModel(request.PaymentType);

        await _repository.AddAsync(returnValue, cancellationToken);

        return returnValue.Id;
    }
}

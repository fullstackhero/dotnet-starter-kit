using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerModeOfPayment;

public class GetAllCustomerModeOfPaymentReques : IRequest<List<CustomerModeOfPaymentModel>>
{
    public GetAllCustomerModeOfPaymentReques()
    {
    }

    public class GetAllCustomerModeOfPaymentRequesHandler : IRequestHandler<GetAllCustomerModeOfPaymentReques, List<CustomerModeOfPaymentModel>>
    {
        private readonly IRepositoryWithEvents<CustomerModeOfPaymentModel> _repository;

        public GetAllCustomerModeOfPaymentRequesHandler(IRepositoryWithEvents<CustomerModeOfPaymentModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<CustomerModeOfPaymentModel>> Handle(GetAllCustomerModeOfPaymentReques request, CancellationToken cancellationToken)
        {
            List<CustomerModeOfPaymentModel> returnValue = new List<CustomerModeOfPaymentModel>();
            returnValue = await _repository.ListAsync(cancellationToken);
            return returnValue;
        }
    }
}

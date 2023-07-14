using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerPolicyStatus;

public class GetAllCustomerPolicyStatusReques : IRequest<List<CustomerPolicyStatusModel>>
{
    public GetAllCustomerPolicyStatusReques()
    {
    }

    public class GetAllCustomerPolicyStatusRequesHandler : IRequestHandler<GetAllCustomerPolicyStatusReques, List<CustomerPolicyStatusModel>>
    {
        private readonly IRepositoryWithEvents<CustomerPolicyStatusModel> _repository;

        public GetAllCustomerPolicyStatusRequesHandler(IRepositoryWithEvents<CustomerPolicyStatusModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<CustomerPolicyStatusModel>> Handle(GetAllCustomerPolicyStatusReques request, CancellationToken cancellationToken)
        {
            List<CustomerPolicyStatusModel> returnValue = new List<CustomerPolicyStatusModel>();
            returnValue = await _repository.ListAsync(cancellationToken);
            return returnValue;
        }
    }
}

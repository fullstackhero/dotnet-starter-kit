using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Customer;
public class GetAllCustomerRequest : IRequest<List<CustomerDetailsModel>>
{
    public GetAllCustomerRequest()
    {

    }

    public class GetAllCustomerRequestHandler : IRequestHandler<GetAllCustomerRequest, List<CustomerDetailsModel>>
    {
        private readonly IRepositoryWithEvents<CustomerDetailsModel> _repository;

        public GetAllCustomerRequestHandler(IRepositoryWithEvents<CustomerDetailsModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<CustomerDetailsModel>> Handle(GetAllCustomerRequest request, CancellationToken cancellationToken)
        {
            List<CustomerDetailsModel> customers = new List<CustomerDetailsModel>();
            customers = await _repository.ListAsync(cancellationToken);
            return customers;
        }
    }
}

using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerCompanyName;

public class GetAllCustomerCompanyNameReques : IRequest<List<CustomerCompanyNameModel>>
{
    public GetAllCustomerCompanyNameReques()
    {
    }

    public class GetAllCustomerCompanyNameRequesHandler : IRequestHandler<GetAllCustomerCompanyNameReques, List<CustomerCompanyNameModel>>
    {
        private readonly IRepositoryWithEvents<CustomerCompanyNameModel> _repository;

        public GetAllCustomerCompanyNameRequesHandler(IRepositoryWithEvents<CustomerCompanyNameModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<CustomerCompanyNameModel>> Handle(GetAllCustomerCompanyNameReques request, CancellationToken cancellationToken)
        {
            List<CustomerCompanyNameModel> invoices = new List<CustomerCompanyNameModel>();
            invoices = await _repository.ListAsync(cancellationToken);
            return invoices;
        }
    }
}

using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
public class GetAllAccountRequest : IRequest<List<AccountDetailsModel>>
{
    public GetAllAccountRequest()
    {

    }

    public class GetAllAccountRequestHandler : IRequestHandler<GetAllAccountRequest, List<AccountDetailsModel>>
    {
        private readonly IRepositoryWithEvents<AccountDetailsModel> _repository;

        public GetAllAccountRequestHandler(IRepositoryWithEvents<AccountDetailsModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<AccountDetailsModel>> Handle(GetAllAccountRequest request, CancellationToken cancellationToken)
        {
            List<AccountDetailsModel> accounts = new List<AccountDetailsModel>();
            accounts = await _repository.ListAsync(cancellationToken);


            return accounts;
        }
    }
}

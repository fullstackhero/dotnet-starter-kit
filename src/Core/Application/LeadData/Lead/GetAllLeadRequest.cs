using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
public class GetAllLeadRequest : IRequest<List<LeadDetailsModel>>
{
    public GetAllLeadRequest()
    {

    }
    public class GetAllLeadRequestHandler : IRequestHandler<GetAllLeadRequest, List<LeadDetailsModel>>
    {
        private readonly IRepositoryWithEvents<LeadDetailsModel> _repository;

        public GetAllLeadRequestHandler(IRepositoryWithEvents<LeadDetailsModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<LeadDetailsModel>> Handle(GetAllLeadRequest request, CancellationToken cancellationToken)
        {
            List<LeadDetailsModel> leads = new List<LeadDetailsModel>();
            leads = await _repository.ListAsync(cancellationToken);


            return leads;
        }
    }
}

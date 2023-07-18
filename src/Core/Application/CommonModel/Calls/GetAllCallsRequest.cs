using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
public class GetAllCallsRequest : IRequest<List<CallsModel>>
{
    public GetAllCallsRequest()
    {

    }

    public class GetAllCallsRequestHandler : IRequestHandler<GetAllCallsRequest, List<CallsModel>>
    {
        private readonly IRepositoryWithEvents<CallsModel> _repository;

        public GetAllCallsRequestHandler(IRepositoryWithEvents<CallsModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<CallsModel>> Handle(GetAllCallsRequest request, CancellationToken cancellationToken)
        {
            List<CallsModel> calls = new List<CallsModel>();
            calls = await _repository.ListAsync(cancellationToken);


            return calls;
        }
    }
}

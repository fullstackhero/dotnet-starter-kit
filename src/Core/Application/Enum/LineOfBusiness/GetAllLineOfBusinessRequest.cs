using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.InvoiceStatus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.LineOfBusiness;
public class GetAllLineOfBusinessRequest : IRequest<List<LineOfBusinessModel>>
{
    public GetAllLineOfBusinessRequest()
    {
    }

    public class GetAllLineOfBusinessRequestHandler : IRequestHandler<GetAllLineOfBusinessRequest, List<LineOfBusinessModel>>
    {
        private readonly IRepositoryWithEvents<LineOfBusinessModel> _repository;

        public GetAllLineOfBusinessRequestHandler(IRepositoryWithEvents<LineOfBusinessModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<LineOfBusinessModel>> Handle(GetAllLineOfBusinessRequest request, CancellationToken cancellationToken)
        {
            List<LineOfBusinessModel> invoices = new List<LineOfBusinessModel>();
            invoices = await _repository.ListAsync(cancellationToken);
            return invoices;
        }
    }
}

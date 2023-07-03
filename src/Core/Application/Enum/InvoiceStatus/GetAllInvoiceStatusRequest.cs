using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.InvoiceStatus;
public class GetAllInvoiceStatusRequest : IRequest<List<InvoiceStatusModel>>
{
    public GetAllInvoiceStatusRequest()
    {
            
    }

    public class GetAllInvoiceStatusRequestHandler : IRequestHandler<GetAllInvoiceStatusRequest, List<InvoiceStatusModel>>
    {
        private readonly IRepositoryWithEvents<InvoiceStatusModel> _repository;

        public GetAllInvoiceStatusRequestHandler(IRepositoryWithEvents<InvoiceStatusModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<InvoiceStatusModel>> Handle(GetAllInvoiceStatusRequest request, CancellationToken cancellationToken)
        {
            List<InvoiceStatusModel> invoices = new List<InvoiceStatusModel>();
            invoices = await _repository.ListAsync(cancellationToken);
            return invoices;
        }
    }
}

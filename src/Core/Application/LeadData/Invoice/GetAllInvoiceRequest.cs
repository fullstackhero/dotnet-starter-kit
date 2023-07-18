using FL_CRMS_ERP_WEBAPI.Application.LeadData.Contact;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Invoice;
public class GetAllInvoiceRequest : IRequest<List<InvoiceDetailsModel>>
{
    public GetAllInvoiceRequest()
    {

    }

    public class GetAllInvoiceRequestHandler : IRequestHandler<GetAllInvoiceRequest, List<InvoiceDetailsModel>>
    {
        private readonly IRepositoryWithEvents<InvoiceDetailsModel> _repository;

        public GetAllInvoiceRequestHandler(IRepositoryWithEvents<InvoiceDetailsModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<InvoiceDetailsModel>> Handle(GetAllInvoiceRequest request, CancellationToken cancellationToken)
        {
            List<InvoiceDetailsModel> invoices = new List<InvoiceDetailsModel>();
            invoices = await _repository.ListAsync(cancellationToken);
            return invoices;
        }
    }
}

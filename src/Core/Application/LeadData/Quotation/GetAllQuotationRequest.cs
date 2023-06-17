using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Quotation;
public class GetAllQuotationRequest : IRequest<List<QuotationDetailsModel>>
{
    public GetAllQuotationRequest()
    {

    }

    public class GetAllQuotationRequestHandler : IRequestHandler<GetAllQuotationRequest, List<QuotationDetailsModel>>
    {
        private readonly IRepositoryWithEvents<QuotationDetailsModel> _repository;

        public GetAllQuotationRequestHandler(IRepositoryWithEvents<QuotationDetailsModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<QuotationDetailsModel>> Handle(GetAllQuotationRequest request, CancellationToken cancellationToken)
        {
            List<QuotationDetailsModel> quotations = new List<QuotationDetailsModel>();
            quotations = await _repository.ListAsync(cancellationToken);


            return quotations;
        }
    }
}

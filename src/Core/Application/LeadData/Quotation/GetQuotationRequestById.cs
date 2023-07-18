using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Quotation;
public class GetQuotationRequestById : IRequest<QuotationDto>
{
    public Guid Id { get; set; }

    public GetQuotationRequestById(Guid id) => Id = id;
}

public class QuotationByIdSpec : Specification<QuotationDetailsModel, QuotationDto>, ISingleResultSpecification
{
    public QuotationByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetQuotationRequestHandler : IRequestHandler<GetQuotationRequestById, QuotationDto>
{
    private readonly IRepository<QuotationDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public GetQuotationRequestHandler(IRepository<QuotationDetailsModel> repository, IStringLocalizer<GetQuotationRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<QuotationDto> Handle(GetQuotationRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<QuotationDetailsModel, QuotationDto>)new QuotationByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Quotation {0} Not Found.", request.Id]);
}

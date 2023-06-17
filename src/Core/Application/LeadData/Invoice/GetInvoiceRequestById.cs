using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Invoice;
public class GetInvoiceRequestById : IRequest<InvoiceDto>
{
    public Guid Id { get; set; }

    public GetInvoiceRequestById(Guid id) => Id = id;
}

public class InvoiceByIdSpec : Specification<InvoiceDetailsModel, InvoiceDto>, ISingleResultSpecification
{
    public InvoiceByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetInvoiceRequestHandler : IRequestHandler<GetInvoiceRequestById, InvoiceDto>
{
    private readonly IRepository<InvoiceDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public GetInvoiceRequestHandler(IRepository<InvoiceDetailsModel> repository, IStringLocalizer<GetInvoiceRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<InvoiceDto> Handle(GetInvoiceRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<InvoiceDetailsModel, InvoiceDto>)new InvoiceByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Invoice {0} Not Found.", request.Id]);
}
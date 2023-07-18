using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Quotation;
public class DeleteQuotationRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteQuotationRequestById(Guid id) => Id = id;
}

public class DeleteQuotationRequestHandler : IRequestHandler<DeleteQuotationRequestById, Guid>
{
    private readonly IRepository<QuotationDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteQuotationRequestHandler(IRepository<QuotationDetailsModel> repository, IStringLocalizer<DeleteAccountRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteQuotationRequestById request, CancellationToken cancellationToken)
    {
        var quotation = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = quotation ?? throw new NotFoundException(_t["Quotation {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        quotation.DomainEvents.Add(EntityDeletedEvent.WithEntity(quotation));

        await _repository.DeleteAsync(quotation, cancellationToken);

        return request.Id;
    }
}

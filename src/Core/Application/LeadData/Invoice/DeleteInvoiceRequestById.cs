using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Invoice;
public class DeleteInvoiceRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteInvoiceRequestById(Guid id) => Id = id;
}

public class DeleteInvoiceRequestHandler : IRequestHandler<DeleteInvoiceRequestById, Guid>
{
    private readonly IRepository<InvoiceDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteInvoiceRequestHandler(IRepository<InvoiceDetailsModel> repository, IStringLocalizer<DeleteAccountRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteInvoiceRequestById request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = invoice ?? throw new NotFoundException(_t["Invoice {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        invoice.DomainEvents.Add(EntityDeletedEvent.WithEntity(invoice));

        await _repository.DeleteAsync(invoice, cancellationToken);

        return request.Id;
    }
}

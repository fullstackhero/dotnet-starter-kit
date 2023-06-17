using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Contact;
public class DeleteContactRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteContactRequestById(Guid id) => Id = id;
}

public class DeleteContactRequestHandler : IRequestHandler<DeleteContactRequestById, Guid>
{
    private readonly IRepository<ContactDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteContactRequestHandler(IRepository<ContactDetailsModel> repository, IStringLocalizer<DeleteContactRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteContactRequestById request, CancellationToken cancellationToken)
    {
        var contact = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = contact ?? throw new NotFoundException(_t["Contact {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        contact.DomainEvents.Add(EntityDeletedEvent.WithEntity(contact));

        await _repository.DeleteAsync(contact, cancellationToken);

        return request.Id;
    }
}
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.Identity;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.Identity.Users.PersonalDetails;
public class DeletePersonalRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeletePersonalRequestById(Guid id) => Id = id;
}

public class DeletePersonalRequestHandler : IRequestHandler<DeletePersonalRequestById, Guid>
{
    private readonly IRepository<PersonalDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public DeletePersonalRequestHandler(IRepository<PersonalDetailsModel> repository, IStringLocalizer<DeleteAccountRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeletePersonalRequestById request, CancellationToken cancellationToken)
    {
        var personal = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = personal ?? throw new NotFoundException(_t["User {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        personal.DomainEvents.Add(EntityDeletedEvent.WithEntity(personal));

        await _repository.DeleteAsync(personal, cancellationToken);

        return request.Id;
    }
}

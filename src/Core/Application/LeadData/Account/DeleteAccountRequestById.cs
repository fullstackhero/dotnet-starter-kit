using FL_CRMS_ERP_WEBAPI.Application.Catalog.Products;
using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
public class DeleteAccountRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteAccountRequestById(Guid id) => Id = id;
}

public class DeleteAccountRequestHandler : IRequestHandler<DeleteAccountRequestById, Guid>
{
    private readonly IRepository<AccountDetailsModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteAccountRequestHandler(IRepository<AccountDetailsModel> repository, IStringLocalizer<DeleteAccountRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteAccountRequestById request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = account ?? throw new NotFoundException(_t["Account {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        account.DomainEvents.Add(EntityDeletedEvent.WithEntity(account));

        await _repository.DeleteAsync(account, cancellationToken);

        return request.Id;
    }
}

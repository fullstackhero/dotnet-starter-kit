using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
public class DeleteCallRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteCallRequestById(Guid id) => Id = id;
}

public class DeleteCallRequestByIdHandler : IRequestHandler<DeleteCallRequestById, Guid>
{
    private readonly IRepository<CallsModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteCallRequestByIdHandler(IRepository<CallsModel> repository, IStringLocalizer<DeleteCallRequestByIdHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteCallRequestById request, CancellationToken cancellationToken)
    {
        var calls = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = calls ?? throw new NotFoundException(_t["Call {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        calls.DomainEvents.Add(EntityDeletedEvent.WithEntity(calls));

        await _repository.DeleteAsync(calls, cancellationToken);

        return request.Id;
    }
}
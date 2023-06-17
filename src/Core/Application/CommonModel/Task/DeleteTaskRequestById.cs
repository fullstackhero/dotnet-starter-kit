using FL_CRMS_ERP_WEBAPI.Application.Catalog.Products;
using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Task;
public class DeleteTaskRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteTaskRequestById(Guid id) => Id = id;
}

public class DeleteTaskRequestHandler : IRequestHandler<DeleteTaskRequestById, Guid>
{
    private readonly IRepository<TaskModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteTaskRequestHandler(IRepository<TaskModel> repository, IStringLocalizer<DeleteTaskRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteTaskRequestById request, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = task ?? throw new NotFoundException(_t["Task {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        task.DomainEvents.Add(EntityDeletedEvent.WithEntity(task));

        await _repository.DeleteAsync(task, cancellationToken);

        return request.Id;
    }
}

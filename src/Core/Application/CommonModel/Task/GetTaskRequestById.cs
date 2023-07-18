using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Task;
public class GetTaskRequestById : IRequest<TaskDto>
{
    public Guid Id { get; set; }

    public GetTaskRequestById(Guid id) => Id = id;
}

public class TaskByIdSpec : Specification<TaskModel, TaskDto>, ISingleResultSpecification
{
    public TaskByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetTaskRequestHandler : IRequestHandler<GetTaskRequestById, TaskDto>
{
    private readonly IRepository<TaskModel> _repository;
    private readonly IStringLocalizer _t;

    public GetTaskRequestHandler(IRepository<TaskModel> repository, IStringLocalizer<GetTaskRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<TaskDto> Handle(GetTaskRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<TaskModel, TaskDto>)new TaskByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Task {0} Not Found.", request.Id]);
}

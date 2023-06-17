using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Task;
public class CreateTaskRequest : IRequest<DefaultIdType>
{
    public Guid TaskOwnerId { get; set; }
    public string? Subject { get; set; }
    public DateTime? DueDate { get; set; }
    public string? ContactName { get; set; }
    public Guid WhoId { get; set; }
    public string? RelatedTo { get; set; }
    public Guid WhatId { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public DateTime? ClosedTime { get; set; }
    public string? Tag { get; set; }
    public string? Description { get; set; }
    public DateTime? Remainder { get; set; }
}

public class CreateTaskRequestHandler : IRequestHandler<CreateTaskRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<TaskModel> _repository;

    public CreateTaskRequestHandler(IRepositoryWithEvents<TaskModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = new TaskModel(request.TaskOwnerId, request.Subject, request.DueDate, request.ContactName, request.WhoId,
            request.RelatedTo, request.WhatId, request.Status, request.Priority, request.ClosedTime, request.Tag, request.Description, request.Remainder);

        await _repository.AddAsync(task, cancellationToken);

        return task.Id;
    }
}

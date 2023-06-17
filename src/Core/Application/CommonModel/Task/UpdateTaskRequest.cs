using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Task;
public class UpdateTaskRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
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

    public class UpdateTaskRequestHandler : IRequestHandler<UpdateTaskRequest, Guid>
    {
        // Add Domain Events automatically by using IRepositoryWithEvents
        private readonly IRepositoryWithEvents<TaskModel> _repository;
        private readonly IStringLocalizer _t;

        public UpdateTaskRequestHandler(IRepositoryWithEvents<TaskModel> repository, IStringLocalizer<UpdateTaskRequestHandler> localizer) =>
            (_repository, _t) = (repository, localizer);

        public async Task<Guid> Handle(UpdateTaskRequest request, CancellationToken cancellationToken)
        {
            var notes = await _repository.GetByIdAsync(request.Id, cancellationToken);

            _ = notes
            ?? throw new NotFoundException(_t["Task {0} Not Found.", request.Id]);

            notes.Update(request.TaskOwnerId, request.Subject, request.DueDate, request.ContactName, request.WhoId,
                request.RelatedTo, request.WhatId, request.Status, request.Priority, request.ClosedTime, request.Tag, request.Description, request.Remainder);

            await _repository.UpdateAsync(notes, cancellationToken);

            return request.Id;
        }
    }
}

using FL_CRMS_ERP_WEBAPI.Application.Catalog.Products;
using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Events;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Meeting;
public class DeleteMeetingRequestById : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteMeetingRequestById(Guid id) => Id = id;
}

public class DeleteMeetingRequestHandler : IRequestHandler<DeleteMeetingRequestById, Guid>
{
    private readonly IRepository<MeetingModel> _repository;
    private readonly IStringLocalizer _t;

    public DeleteMeetingRequestHandler(IRepository<MeetingModel> repository, IStringLocalizer<DeleteMeetingRequestHandler> localizer) =>
        (_repository, _t) = (repository, localizer);

    public async Task<Guid> Handle(DeleteMeetingRequestById request, CancellationToken cancellationToken)
    {
        var meeting = await _repository.GetByIdAsync(request.Id, cancellationToken);

        _ = meeting ?? throw new NotFoundException(_t["Meeting {0} Not Found."]);

        // Add Domain Events to be raised after the commit
        meeting.DomainEvents.Add(EntityDeletedEvent.WithEntity(meeting));

        await _repository.DeleteAsync(meeting, cancellationToken);

        return request.Id;
    }
}
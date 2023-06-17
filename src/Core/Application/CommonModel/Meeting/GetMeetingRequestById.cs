using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Meeting;
public class GetMeetingRequestById : IRequest<MeetingDto>
{
    public Guid Id { get; set; }

    public GetMeetingRequestById(Guid id) => Id = id;

}

public class MeetingByIdSpec : Specification<MeetingModel, MeetingDto>, ISingleResultSpecification
{
    public MeetingByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetMeetingRequestHandler : IRequestHandler<GetMeetingRequestById, MeetingDto>
{
    private readonly IRepository<MeetingModel> _repository;
    private readonly IStringLocalizer _t;

    public GetMeetingRequestHandler(IRepository<MeetingModel> repository, IStringLocalizer<GetMeetingRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<MeetingDto> Handle(GetMeetingRequestById request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<MeetingModel, MeetingDto>)new MeetingByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Meeting {0} Not Found.", request.Id]);
}

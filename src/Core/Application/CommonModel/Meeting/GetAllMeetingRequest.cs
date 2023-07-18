using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Meeting;
public class GetAllMeetingRequest : IRequest<List<MeetingModel>>
{
    public GetAllMeetingRequest()
    {

    }

    public class GetAllMeetingRequestHandler : IRequestHandler<GetAllMeetingRequest, List<MeetingModel>>
    {
        private readonly IRepositoryWithEvents<MeetingModel> _repository;

        public GetAllMeetingRequestHandler(IRepositoryWithEvents<MeetingModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<MeetingModel>> Handle(GetAllMeetingRequest request, CancellationToken cancellationToken)
        {
            List<MeetingModel> meetings = new List<MeetingModel>();
            meetings = await _repository.ListAsync(cancellationToken);


            return meetings;
        }
    }
}

using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Meeting;
public class UpdateMeetingRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string? MeetingTitle { get; set; }
    public string? Location { get; set; }
    public bool Allday { get; set; }
    public DateTime? FromDate { get; set; }
    //public DateTime? FromTime { get; set; }
    public DateTime? ToDate { get; set; }

    public string? Host { get; set; }
    public Guid MeetingOwnerId { get; set; }
    public string? ContactName { get; set; }
    public Guid WhoId { get; set; }
    public string? RelatedTo { get; set; }
    public Guid WhatId { get; set; }
    public bool Repeat { get; set; }

    public string? Description { get; set; }
    public DateTime? CheckInTime { get; set; }
    public string? CheckInById { get; set; }
    public string? CheckInComment { get; set; }
    public string? CheckInSubLocality { get; set; }
    public string? CheckInCity { get; set; }
    public string? CheckInState { get; set; }
    public string? CheckInCountry { get; set; }
    public string? ZipCode { get; set; }
    public string? CheckInAddress { get; set; }
    public bool CheckedInStatus { get; set; }
    public string? Tag { get; set; }
    //public long RecordId { get; set; }
    //public int IsDeleted { get; set; }
    //public string CompanyId { get; set; }
    public string[]? Participants { get; set; }
    public int RemindMe { get; set; }

    public class UpdateMeetingRequestHandler : IRequestHandler<UpdateMeetingRequest, Guid>
    {
        // Add Domain Events automatically by using IRepositoryWithEvents
        private readonly IRepositoryWithEvents<MeetingModel> _repository;
        private readonly IStringLocalizer _t;

        public UpdateMeetingRequestHandler(IRepositoryWithEvents<MeetingModel> repository, IStringLocalizer<UpdateMeetingRequestHandler> localizer) =>
            (_repository, _t) = (repository, localizer);

        public async Task<Guid> Handle(UpdateMeetingRequest request, CancellationToken cancellationToken)
        {
            var meeting = await _repository.GetByIdAsync(request.Id, cancellationToken);

            _ = meeting
            ?? throw new NotFoundException(_t["Meeting {0} Not Found.", request.Id]);

            meeting.Update(request.MeetingTitle, request.Location, request.Allday, request.FromDate, request.ToDate,
                request.Host, request.MeetingOwnerId, request.ContactName, request.WhoId, request.RelatedTo, request.WhatId, request.Repeat,
                request.Description, request.CheckInTime, request.CheckInById, request.CheckInComment, request.CheckInSubLocality, request.CheckInCity,
                request.CheckInState, request.CheckInCountry, request.ZipCode, request.CheckInAddress, request.CheckedInStatus, request.Tag, request.Participants, request.RemindMe);

            await _repository.UpdateAsync(meeting, cancellationToken);

            return request.Id;
        }
    }
}

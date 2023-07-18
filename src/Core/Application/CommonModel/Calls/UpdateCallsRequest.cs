using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
public class UpdateCallsRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public Guid CallOwnerId { get; set; }

    public string? Subject { get; set; }
    public string? CallType { get; set; }
    public string? CallPurpose { get; set; }
    public string? ContactName { get; set; }
    public Guid WhoId { get; set; }
    public string? RelatedTo { get; set; }
    public Guid WhatId { get; set; }
    public DateTime? CallStartTime { get; set; }
    //public TimeSpan? CallDuration { get; set; }
    public int CallDurationinSeconds { get; set; }
    public string? Description { get; set; }
    public string? CallResult { get; set; }
    public string? Tag { get; set; }
    public string? OutgoingCallStatus { get; set; }
    public bool SchedulinginCRM { get; set; }
    public string? CallAgenda { get; set; }
    public string? CallerId { get; set; }
    public string? DialledNumber { get; set; }
    public string? RemainderTime { get; set; }
    public string? TimeZone { get; set; }

    public class UpdateCallsRequestHandler : IRequestHandler<UpdateCallsRequest, Guid>
    {
        // Add Domain Events automatically by using IRepositoryWithEvents
        private readonly IRepositoryWithEvents<CallsModel> _repository;
        private readonly IStringLocalizer _t;

        public UpdateCallsRequestHandler(IRepositoryWithEvents<CallsModel> repository, IStringLocalizer<UpdateCallsRequestHandler> localizer) =>
            (_repository, _t) = (repository, localizer);

        public async Task<Guid> Handle(UpdateCallsRequest request, CancellationToken cancellationToken)
        {
            var calls = await _repository.GetByIdAsync(request.Id, cancellationToken);

            _ = calls
            ?? throw new NotFoundException(_t["Call {0} Not Found.", request.Id]);

            calls.Update(request.CallOwnerId, request.Subject, request.CallType, request.CallPurpose, request.ContactName, request.WhoId, request.RelatedTo, request.WhatId, request.CallStartTime,
                request.CallDurationinSeconds, request.Description, request.CallResult, request.Tag, request.OutgoingCallStatus, request.SchedulinginCRM, request.CallAgenda, request.CallerId, request.DialledNumber, request.RemainderTime, request.TimeZone);

            await _repository.UpdateAsync(calls, cancellationToken);

            return request.Id;
        }
    }
}

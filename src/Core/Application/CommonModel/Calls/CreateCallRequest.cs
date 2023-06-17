using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Calls;
public class CreateCallRequest : IRequest<DefaultIdType>
{
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
}

public class CreateCallRequestHandler : IRequestHandler<CreateCallRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<CallsModel> _repository;

    public CreateCallRequestHandler(IRepositoryWithEvents<CallsModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateCallRequest request, CancellationToken cancellationToken)
    {
        var calls = new CallsModel(request.CallOwnerId, request.Subject, request.CallType, request.CallPurpose, request.ContactName, request.WhoId, request.RelatedTo,
            request.WhatId, request.CallStartTime, request.CallDurationinSeconds, request.Description, request.CallResult, request.Tag, request.OutgoingCallStatus, request.SchedulinginCRM, request.CallAgenda,
            request.CallerId, request.DialledNumber, request.RemainderTime, request.TimeZone);

        await _repository.AddAsync(calls, cancellationToken);

        return calls.Id;
    }
}

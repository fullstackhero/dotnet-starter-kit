using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
public class CallsModel : AuditableEntity, IAggregateRoot
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

    public CallsModel(Guid callOwnerId, string? subject, string? callType, string? callPurpose, string? contactName, Guid whoId, string? relatedTo, Guid whatId, DateTime? callStartTime, int callDurationinSeconds, string? description, string? callResult, string? tag, string? outgoingCallStatus, bool schedulinginCRM, string? callAgenda, string? callerId, string? dialledNumber, string? remainderTime, string? timeZone)
    {
        CallOwnerId = callOwnerId;
        Subject = subject;
        CallType = callType;
        CallPurpose = callPurpose;
        ContactName = contactName;
        WhoId = whoId;
        RelatedTo = relatedTo;
        WhatId = whatId;
        CallStartTime = callStartTime;
        CallDurationinSeconds = callDurationinSeconds;
        Description = description;
        CallResult = callResult;
        Tag = tag;
        OutgoingCallStatus = outgoingCallStatus;
        SchedulinginCRM = schedulinginCRM;
        CallAgenda = callAgenda;
        CallerId = callerId;
        DialledNumber = dialledNumber;
        RemainderTime = remainderTime;
        TimeZone = timeZone;
    }

    public CallsModel Update(Guid callOwnerId, string? subject, string? callType, string? callPurpose, string? contactName, Guid whoId, string? relatedTo, Guid whatId, DateTime? callStartTime, int callDurationinSeconds, string? description, string? callResult, string? tag, string? outgoingCallStatus, bool schedulinginCRM, string? callAgenda, string? callerId, string? dialledNumber, string? remainderTime, string? timeZone)
    {
        if (callOwnerId != Guid.Empty && !CallOwnerId.Equals(callOwnerId)) CallOwnerId = callOwnerId;
        if (subject is not null && Subject?.Equals(subject) is not true) Subject = subject;
        if (callType is not null && CallType?.Equals(callType) is not true) CallType = callType;
        if (callPurpose is not null && CallPurpose?.Equals(callPurpose) is not true) CallPurpose = callPurpose;
        if (contactName is not null && ContactName?.Equals(contactName) is not true) ContactName = contactName;
        if (whoId != Guid.Empty && !WhoId.Equals(whoId)) WhoId = whoId;
        if (relatedTo is not null && RelatedTo?.Equals(relatedTo) is not true) RelatedTo = relatedTo;
        if (whatId != Guid.Empty && !WhatId.Equals(whatId)) WhatId = whatId;
        if (callStartTime is not null && CallStartTime?.Equals(callStartTime) is not true) CallStartTime = callStartTime;
        if (CallDurationinSeconds != callDurationinSeconds) CallDurationinSeconds = callDurationinSeconds;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (callResult is not null && CallResult?.Equals(callResult) is not true) CallResult = callResult;
        if (tag is not null && Tag?.Equals(tag) is not true) Tag = tag;
        if (outgoingCallStatus is not null && OutgoingCallStatus?.Equals(outgoingCallStatus) is not true) OutgoingCallStatus = outgoingCallStatus;
        if (SchedulinginCRM != schedulinginCRM) SchedulinginCRM = schedulinginCRM;
        if (callAgenda is not null && CallAgenda?.Equals(callAgenda) is not true) CallAgenda = callAgenda;
        if (callerId is not null && CallerId?.Equals(callerId) is not true) CallerId = callerId;
        if (dialledNumber is not null && DialledNumber?.Equals(dialledNumber) is not true) DialledNumber = dialledNumber;
        if (remainderTime is not null && RemainderTime?.Equals(remainderTime) is not true) RemainderTime = remainderTime;
        if (timeZone is not null && TimeZone?.Equals(timeZone) is not true) TimeZone = timeZone;
        return this;
    }
}

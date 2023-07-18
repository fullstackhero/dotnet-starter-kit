using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
public class MeetingModel : AuditableEntity, IAggregateRoot
{
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
    public int? RemindMe { get; set; }

    public MeetingModel(string? meetingTitle, string? location, bool allday, DateTime? fromDate, DateTime? toDate, string? host, Guid meetingOwnerId, string? contactName, Guid whoId, string? relatedTo, Guid whatId, bool repeat, string? description, DateTime? checkInTime, string? checkInById, string? checkInComment, string? checkInSubLocality, string? checkInCity, string? checkInState, string? checkInCountry, string? zipCode, string? checkInAddress, bool checkedInStatus, string? tag, string[]? participants, int? remindMe)
    {
        MeetingTitle = meetingTitle;
        Location = location;
        Allday = allday;
        FromDate = fromDate;
        ToDate = toDate;
        Host = host;
        MeetingOwnerId = meetingOwnerId;
        ContactName = contactName;
        WhoId = whoId;
        RelatedTo = relatedTo;
        WhatId = whatId;
        Repeat = repeat;
        Description = description;
        CheckInTime = checkInTime;
        CheckInById = checkInById;
        CheckInComment = checkInComment;
        CheckInSubLocality = checkInSubLocality;
        CheckInCity = checkInCity;
        CheckInState = checkInState;
        CheckInCountry = checkInCountry;
        ZipCode = zipCode;
        CheckInAddress = checkInAddress;
        CheckedInStatus = checkedInStatus;
        Tag = tag;
        Participants = participants;
        RemindMe = remindMe;
    }

    public MeetingModel Update(string? meetingTitle, string? location, bool allday, DateTime? fromDate, DateTime? toDate, string? host, Guid meetingOwnerId, string? contactName, Guid whoId, string? relatedTo, Guid whatId, bool repeat, string? description, DateTime? checkInTime, string? checkInById, string? checkInComment, string? checkInSubLocality, string? checkInCity, string? checkInState, string? checkInCountry, string? zipCode, string? checkInAddress, bool checkedInStatus, string? tag, string[]? participants, int remindMe)
    {
        if (meetingTitle is not null && MeetingTitle?.Equals(meetingTitle) is not true) MeetingTitle = meetingTitle;
        if (location is not null && Location?.Equals(location) is not true) Location = location;
        if (Allday != allday) Allday = allday;
        if (fromDate is not null && FromDate?.Equals(fromDate) is not true) FromDate = fromDate;
        if (toDate is not null && ToDate?.Equals(toDate) is not true) ToDate = toDate;
        if (host is not null && Host?.Equals(host) is not true) Host = host;
        if (toDate is not null && ToDate?.Equals(toDate) is not true) ToDate = toDate;
        if (meetingOwnerId != Guid.Empty && !MeetingOwnerId.Equals(meetingOwnerId)) MeetingOwnerId = meetingOwnerId;
        if (contactName is not null && ContactName?.Equals(contactName) is not true) ContactName = contactName;
        if (whoId != Guid.Empty && !WhoId.Equals(whoId)) WhoId = whoId;
        if (relatedTo is not null && RelatedTo?.Equals(relatedTo) is not true) RelatedTo = relatedTo;
        if (whatId != Guid.Empty && !WhatId.Equals(whatId)) WhatId = whatId;
        if (Repeat != repeat) Repeat = repeat;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (checkInTime is not null && CheckInTime?.Equals(checkInTime) is not true) CheckInTime = checkInTime;
        if (checkInById is not null && CheckInById?.Equals(checkInById) is not true) CheckInById = checkInById;
        if (checkInComment is not null && CheckInComment?.Equals(checkInComment) is not true) CheckInComment = checkInComment;
        if (checkInSubLocality is not null && CheckInSubLocality?.Equals(checkInSubLocality) is not true) CheckInSubLocality = checkInSubLocality;
        if (checkInCity is not null && CheckInCity?.Equals(checkInCity) is not true) CheckInCity = checkInCity;
        if (checkInState is not null && CheckInState?.Equals(checkInState) is not true) CheckInState = checkInState;
        if (checkInCountry is not null && CheckInCountry?.Equals(checkInCountry) is not true) CheckInCountry = checkInCountry;
        if (zipCode is not null && ZipCode?.Equals(zipCode) is not true) ZipCode = zipCode;
        if (CheckedInStatus != checkedInStatus) CheckedInStatus = checkedInStatus;
        if (checkInAddress is not null && CheckInAddress?.Equals(checkInAddress) is not true) CheckInAddress = checkInAddress;
        if (tag is not null && Tag?.Equals(tag) is not true) Tag = tag;
        if (participants is not null && Participants?.Equals(participants) is not true) Participants = participants;
        if (RemindMe != remindMe) RemindMe = remindMe;
        return this;
    }
}

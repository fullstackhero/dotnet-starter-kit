using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Meeting;
public class MeetingDto
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
}

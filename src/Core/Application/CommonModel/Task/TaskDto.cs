using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Task;
public class TaskDto
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
}

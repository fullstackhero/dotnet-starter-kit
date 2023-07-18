using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
public class TaskModel : AuditableEntity, IAggregateRoot
{
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

    public TaskModel(Guid taskOwnerId, string? subject, DateTime? dueDate, string? contactName, Guid whoId, string? relatedTo, Guid whatId, string? status, string? priority, DateTime? closedTime, string? tag, string? description, DateTime? remainder)
    {
        TaskOwnerId = taskOwnerId;
        Subject = subject;
        DueDate = dueDate;
        ContactName = contactName;
        WhoId = whoId;
        RelatedTo = relatedTo;
        WhatId = whatId;
        Status = status;
        Priority = priority;
        ClosedTime = closedTime;
        Tag = tag;
        Description = description;
        Remainder = remainder;
    }

    public TaskModel Update(Guid taskOwnerId, string? subject, DateTime? dueDate, string? contactName, Guid whoId, string? relatedTo, Guid whatId, string? status, string? priority, DateTime? closedTime, string? tag, string? description, DateTime? remainder)
    {
        if (taskOwnerId != Guid.Empty && !TaskOwnerId.Equals(taskOwnerId)) TaskOwnerId = taskOwnerId;
        if (subject is not null && Subject?.Equals(subject) is not true) Subject = subject;
        if (dueDate is not null && DueDate?.Equals(dueDate) is not true) DueDate = dueDate;
        if (contactName is not null && ContactName?.Equals(contactName) is not true) ContactName = contactName;
        if (whoId != Guid.Empty && !WhoId.Equals(whoId)) WhoId = whoId;
        if (relatedTo is not null && RelatedTo?.Equals(relatedTo) is not true) RelatedTo = relatedTo;
        if (whatId != Guid.Empty && !WhatId.Equals(WhatId)) WhatId = whatId;
        if (status is not null && Status?.Equals(status) is not true) Status = status;
        if (priority is not null && Priority?.Equals(priority) is not true) Priority = priority;
        if (closedTime is not null && ClosedTime?.Equals(closedTime) is not true) ClosedTime = closedTime;
        if (tag is not null && Tag?.Equals(tag) is not true) Tag = tag;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (remainder is not null && Remainder?.Equals(remainder) is not true) Remainder = remainder;
        return this;
    }
}

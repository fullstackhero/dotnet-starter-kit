using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.Enum;
public class InvoiceStatusModel : AuditableEntity, IAggregateRoot
{
    public string? StatusName { get; set; }

    public InvoiceStatusModel(string? statusName)
    {
        StatusName = statusName;
    }
    //public InvoiceStatusModel Update(string? statusName)
    //{
    //    if (statusName is not null && StatusName?.Equals(statusName) is not true) StatusName = statusName;
    //    return this;
    //}
}


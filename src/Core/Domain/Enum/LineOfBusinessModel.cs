using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.Enum;
public class LineOfBusinessModel : AuditableEntity, IAggregateRoot
{
    public string? BusinessName { get; set; }

    public LineOfBusinessModel(string? businessName)
    {
        BusinessName = businessName;
    }
}

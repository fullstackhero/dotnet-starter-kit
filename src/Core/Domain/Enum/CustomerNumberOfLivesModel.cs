using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.Enum;

public class CustomerNumberOfLivesModel : AuditableEntity, IAggregateRoot
{
    public string? TotalLives { get; set; }

    public CustomerNumberOfLivesModel(string? totalLives)
    {
        TotalLives = totalLives;
    }
}

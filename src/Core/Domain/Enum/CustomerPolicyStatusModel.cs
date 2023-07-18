using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.Enum;

public class CustomerPolicyStatusModel : AuditableEntity, IAggregateRoot
{
    public string? PolicyName { get; set; }

    public CustomerPolicyStatusModel(string? policyName)
    {
        PolicyName = policyName;
    }
}


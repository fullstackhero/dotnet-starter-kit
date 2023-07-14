using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.Enum;

public class CustomerModeOfPaymentModel : AuditableEntity, IAggregateRoot
{
    public string? PaymentType { get; set; }

    public CustomerModeOfPaymentModel(string? paymentType)
    {
        PaymentType = paymentType;
    }
}

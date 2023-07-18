using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Domain.Catalog.ChargeAggregate;

public enum ChargeStatus
{
    Initiated = 1,
    Active = 2,
    Stopped = 3,
}
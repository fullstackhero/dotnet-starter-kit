using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Common.Exceptions;

public class EntityCannotBeDeleted : CustomException
{
    public EntityCannotBeDeleted(string message)
    : base(message, null, HttpStatusCode.BadRequest)
    {
    }
}

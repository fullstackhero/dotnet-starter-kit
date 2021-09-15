using System.Collections.Generic;
using System.Net;

namespace DN.WebApi.Application.Exceptions
{
    public class ValidationException : CustomException
    {
        public ValidationException(List<string> errors = default)
            : base("One or more validation failures have occurred.", errors, HttpStatusCode.BadRequest)
        {
        }
    }
}
using System.Net;

namespace DN.WebApi.Application.Common.Exceptions;

public class ValidationException : CustomException
{
    public Dictionary<string, List<string>>? ValidationErrors { get; set; }

    public ValidationException(List<string>? errors = default, Dictionary<string, List<string>>? validationErrors = default)
        : base("Validation Failures Occurred.", errors, HttpStatusCode.BadRequest)
    {
        ValidationErrors = validationErrors;
    }
}
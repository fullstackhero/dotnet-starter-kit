using FSH.Framework.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace FSH.Framework.Web.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        var statusCode = StatusCodes.Status500InternalServerError;

        if (exception is FluentValidation.ValidationException fluentException)
        {
            statusCode = StatusCodes.Status400BadRequest;

            problemDetails.Status = statusCode;
            problemDetails.Title = "Validation error";
            problemDetails.Detail = "One or more validation errors occurred.";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

            var errors = fluentException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            problemDetails.Extensions["errors"] = errors;
        }
        else if (exception is CustomException e)
        {
            statusCode = (int)e.StatusCode;

            problemDetails.Status = statusCode;
            problemDetails.Title = e.GetType().Name;
            problemDetails.Detail = e.Message;

            if (e.ErrorMessages is { Count: > 0 })
            {
                problemDetails.Extensions["errors"] = e.ErrorMessages;
            }
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;

            problemDetails.Status = statusCode;
            problemDetails.Title = "An unexpected error occurred";
            problemDetails.Detail = "An unexpected error occurred. Please try again later.";
        }

        httpContext.Response.StatusCode = statusCode;

        LogContext.PushProperty("exception_title", problemDetails.Title);
        LogContext.PushProperty("exception_detail", problemDetails.Detail);
        LogContext.PushProperty("exception_statusCode", problemDetails.Status);
        LogContext.PushProperty("exception_stackTrace", exception.StackTrace);

        logger.LogError("Exception at {Path} - {StatusCode} {Title}", httpContext.Request.Path.Value?.Replace(Environment.NewLine, string.Empty), statusCode, problemDetails.Title);

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);
        return true;
    }
}

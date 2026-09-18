using System.Diagnostics;
using System;
using System.Globalization;
using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Localization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace FSH.Framework.Web.Exceptions;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IStringLocalizer<SharedResources> localizer,
    IStringLocalizerFactory localizerFactory) : IExceptionHandler
{
    // Returns null for a status with no title of its own. Deliberately NOT a catch-all
    // "Error.Unexpected": that key exists, so ResourceNotFound would be false and a 409 would
    // report "An unexpected error occurred" alongside Status 409 and a Detail describing a
    // perfectly ordinary business-rule conflict — a title contradicting its own status code.
    // A null key keeps the pre-localization behaviour (the exception type name) for every
    // status not translated here, which is at least status-consistent.
    private static string? TitleKeyFor(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => "Error.NotFound",
        HttpStatusCode.Unauthorized => "Error.Unauthorized",
        HttpStatusCode.Forbidden => "Error.Forbidden",
        HttpStatusCode.BadRequest => "Error.BadRequest",
        HttpStatusCode.Conflict => "Error.Conflict",
        _ => null,
    };

    // Resolves the localized Detail for an exception carrying a MessageKey, under the request culture.
    // The [key, args] indexer runs string.Format; a stray '{' in the resx would throw FormatException
    // from inside the handler, so fall back to the (English) message on a format error or a missing key.
    // A null key keeps the literal message. Shared by the CustomException, Unauthorized and NotFound
    // branches so a localized BCL subclass (ILocalizableMessage) translates the same way.
    private string LocalizeDetail(ILocalizableMessage localizable, string fallbackMessage)
    {
        if (localizable.MessageKey is null)
        {
            return fallbackMessage;
        }

        var moduleLocalizer = localizerFactory.Create(localizable.ResourceSource ?? typeof(SharedResources));
        try
        {
            var message = localizable.MessageArgs.Count == 0
                ? moduleLocalizer[localizable.MessageKey]
                : moduleLocalizer[localizable.MessageKey, localizable.MessageArgs.ToArray()];
            return message.ResourceNotFound ? fallbackMessage : message.Value;
        }
        catch (FormatException)
        {
            return fallbackMessage;
        }
    }

    // Writes the localized Detail and, when the exception carries a MessageKey, surfaces that key as a
    // stable machine-readable "code". Detail is prose under the request culture, so a client that needs
    // to branch on a specific error (a terminal state, a dedicated screen, a retry) keys off the code
    // instead of matching text that changes with Accept-Language.
    private void ApplyLocalizedDetail(ProblemDetails problemDetails, ILocalizableMessage localizable, string fallbackMessage)
    {
        problemDetails.Detail = LocalizeDetail(localizable, fallbackMessage);

        if (localizable.MessageKey is not null)
        {
            problemDetails.Extensions["code"] = localizable.MessageKey;
        }
    }

    // UseExceptionHandler sits ABOVE UseHeroLocalization in the pipeline, and
    // RequestLocalizationMiddleware assigns CultureInfo.CurrentUICulture inside its own async frame —
    // an assignment that belongs to that frame's ExecutionContext and is already gone by the time an
    // exception unwinds up to this handler. Every localizer below would therefore resolve under the
    // culture of the host process (the invariant one in a container with no LANG), and answer from the
    // neutral resx no matter what the client asked for. The negotiated culture survives on the request
    // itself, so take it from there and restore the ambient one afterwards.
    //
    // CurrentUICulture only: AddHeroLocalization pins CurrentCulture to invariant on purpose, so that
    // no request can shift numeric or date formatting anywhere in the pipeline.
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var requestUiCulture = httpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture;

        // No feature means the exception escaped before localization ran (CORS, security headers,
        // forwarded headers). Nothing was negotiated, so the ambient culture is all there is.
        if (requestUiCulture is null)
        {
            return await WriteProblemDetailsAsync(httpContext, exception, requestUiCulture: null, cancellationToken).ConfigureAwait(false);
        }

        var previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = requestUiCulture;
        try
        {
            return await WriteProblemDetailsAsync(httpContext, exception, requestUiCulture, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private async ValueTask<bool> WriteProblemDetailsAsync(HttpContext httpContext, Exception exception, CultureInfo? requestUiCulture, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        var statusCode = StatusCodes.Status500InternalServerError;

        if (exception is FluentValidation.ValidationException fluentException)
        {
            statusCode = StatusCodes.Status400BadRequest;

            problemDetails.Status = statusCode;
            problemDetails.Title = localizer["Error.Validation"];
            problemDetails.Detail = localizer["Error.Validation.Detail"];
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

            var titleKey = TitleKeyFor(e.StatusCode);
            var title = titleKey is null ? null : localizer[titleKey];
            problemDetails.Title = title is null || title.ResourceNotFound ? e.GetType().Name : title.Value;

            ApplyLocalizedDetail(problemDetails, e, e.Message);

            if (e.ErrorMessages is { Count: > 0 })
            {
                problemDetails.Extensions["errors"] = e.ErrorMessages;
            }
        }
        else if (exception is UnauthorizedAccessException)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            problemDetails.Status = statusCode;
            problemDetails.Title = localizer["Error.Unauthorized"];
            if (exception is ILocalizableMessage unauthorizedLoc)
            {
                ApplyLocalizedDetail(problemDetails, unauthorizedLoc, exception.Message);
            }
            else
            {
                problemDetails.Detail = exception.Message;
            }
        }
        else if (exception is KeyNotFoundException)
        {
            statusCode = StatusCodes.Status404NotFound;
            problemDetails.Status = statusCode;
            problemDetails.Title = localizer["Error.NotFound"];
            if (exception is ILocalizableMessage notFoundLoc)
            {
                ApplyLocalizedDetail(problemDetails, notFoundLoc, exception.Message);
            }
            else
            {
                problemDetails.Detail = exception.Message;
            }
        }
        else if (exception is BadHttpRequestException badRequest)
        {
            // BadHttpRequestException = malformed request (missing required header/param, unreadable/oversized body).
            // Client error carrying the correct status (usually 400) — honour it instead of falling through to a generic 500.
            statusCode = badRequest.StatusCode;
            problemDetails.Status = statusCode;
            problemDetails.Title = localizer["Error.BadRequest"];
            problemDetails.Detail = badRequest.Message;
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;
            problemDetails.Status = statusCode;
            problemDetails.Title = localizer["Error.Unexpected"];
            problemDetails.Detail = localizer["Error.Unexpected.Detail"];
        }

        httpContext.Response.StatusCode = statusCode;

        // ExceptionHandlerMiddleware clears the response before re-executing, which drops the
        // Content-Language RequestLocalizationMiddleware had already written. Put it back, so a client
        // can tell which culture the prose in this body is in. The invariant culture has an empty name
        // and is not a valid header value.
        if (requestUiCulture is not null && requestUiCulture.Name.Length > 0)
        {
            httpContext.Response.Headers.ContentLanguage = requestUiCulture.Name;
        }

        // Surface trace and correlation IDs so clients/support can correlate errors to traces
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        problemDetails.Extensions["traceId"] = traceId;

        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? httpContext.TraceIdentifier;
        problemDetails.Extensions["correlationId"] = correlationId;

        // Log the raw (English) exception message and type, never the localized ProblemDetails body,
        // so log entries stay culture-independent regardless of the request's negotiated culture.
        // PushProperty returns an IDisposable that pops the property on dispose; scope it to the LogError
        // call so it does not leak onto every subsequent log entry of the request (AsyncLocal contamination).
        var logPath = httpContext.Request.Path.Value?.Replace(Environment.NewLine, string.Empty);
        using (LogContext.PushProperty("exception_type", exception.GetType().Name))
        using (LogContext.PushProperty("exception_detail", exception.Message))
        using (LogContext.PushProperty("exception_statusCode", statusCode))
        using (LogContext.PushProperty("exception_stackTrace", exception.StackTrace))
        {
            logger.LogError("Exception at {Path} - {StatusCode} {Type}", logPath, statusCode, exception.GetType().Name);
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
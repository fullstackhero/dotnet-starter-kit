using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Wrapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Serilog;
using Serilog.Context;
using System.Net;

namespace DN.WebApi.Host.Endpoints;

public static class ErrorResultExtensions
{
    public static ObjectResult ErrorResult(this ControllerBase controller, string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        var services = controller.ControllerContext.HttpContext.RequestServices;
        var currentUser = services.GetRequiredService<ICurrentUser>();
        var localizer = services.GetRequiredService<IStringLocalizer<ControllerBase>>();

        string email = currentUser.GetUserEmail() is string userEmail ? userEmail : "Anonymous";
        var userId = currentUser.GetUserId();
        string tenant = currentUser.GetTenant() ?? string.Empty;
        if (userId != Guid.Empty) LogContext.PushProperty("UserId", userId);
        LogContext.PushProperty("UserEmail", email);
        if (!string.IsNullOrEmpty(tenant)) LogContext.PushProperty("Tenant", tenant);
        string errorId = Guid.NewGuid().ToString();
        LogContext.PushProperty("ErrorId", errorId);
        LogContext.PushProperty("StackTrace", Environment.StackTrace);

        var errorResult = new ErrorResult
        {
            Source = controller.GetType().FullName,
            Exception = message.Trim(),
            ErrorId = errorId,
            SupportMessage = localizer["exceptionmiddleware.supportmessage"]
        };
        errorResult.Messages!.Add(message);

        Log.Error($"{errorResult.Exception} Request failed with Status Code {statusCode} and Error Id {errorId}.");

        return new ObjectResult(errorResult)
        {
            StatusCode = (int)statusCode
        };
    }

    public static ObjectResult ConflictError(this ControllerBase controller, string message) =>
        controller.ErrorResult(message, HttpStatusCode.Conflict);

    public static ObjectResult NotFoundError(this ControllerBase controller, string message) =>
        controller.ErrorResult(message, HttpStatusCode.NotFound);
}
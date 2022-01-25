using FSH.WebApi.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace FSH.WebApi.Infrastructure.Middleware;

public class ResponseLoggingMiddleware : IMiddleware
{
    private readonly ICurrentUser _currentUser;

    public ResponseLoggingMiddleware(ICurrentUser currentUser) => _currentUser = currentUser;

    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        await next(httpContext);
        var originalBody = httpContext.Response.Body;
        using var newBody = new MemoryStream();
        httpContext.Response.Body = newBody;
        string responseBody;
        if (httpContext.Request.Path.ToString().Contains("tokens"))
        {
            responseBody = "[Redacted] Contains Sensitive Information.";
        }
        else
        {
            newBody.Seek(0, SeekOrigin.Begin);
            responseBody = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        }

        string email = _currentUser.GetUserEmail() is string userEmail ? userEmail : "Anonymous";
        var userId = _currentUser.GetUserId();
        string tenant = _currentUser.GetTenant() ?? string.Empty;
        if (userId != Guid.Empty) LogContext.PushProperty("UserId", userId);
        LogContext.PushProperty("UserEmail", email);
        if (!string.IsNullOrEmpty(tenant)) LogContext.PushProperty("Tenant", tenant);
        LogContext.PushProperty("StatusCode", httpContext.Response.StatusCode);
        LogContext.PushProperty("ResponseTimeUTC", DateTime.UtcNow);
        Log.ForContext("ResponseHeaders", httpContext.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), destructureObjects: true)
       .ForContext("ResponseBody", responseBody)
       .Information("HTTP {RequestMethod} Request to {RequestPath} by {RequesterEmail} has Status Code {StatusCode}.", httpContext.Request.Method, httpContext.Request.Path, string.IsNullOrEmpty(email) ? "Anonymous" : email, httpContext.Response.StatusCode);
        newBody.Seek(0, SeekOrigin.Begin);
        await newBody.CopyToAsync(originalBody);
    }
}
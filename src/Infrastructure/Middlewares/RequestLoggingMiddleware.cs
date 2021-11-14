using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DN.WebApi.Infrastructure.Middlewares
{
    public class RequestLoggingMiddleware : IMiddleware
    {
        private readonly ICurrentUser _currentUser;

        public RequestLoggingMiddleware(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            LogContext.PushProperty("RequestTimeUTC", DateTime.UtcNow);
            string requestBody = string.Empty;
            if (httpContext.Request.Path.ToString().Contains("tokens"))
            {
                requestBody = "[Redacted] Contains Sensitive Information.";
            }
            else
            {
                var request = httpContext.Request;

                if (!string.IsNullOrEmpty(request.ContentType)
                    && request.ContentType.StartsWith("application/json"))
                {
                    request.EnableBuffering();
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 4096, true);
                    requestBody = await reader.ReadToEndAsync();

                    // rewind for next middleware.
                    request.Body.Position = 0;
                }
            }

            LogContext.PushProperty("RequestBody", requestBody);
            Log.ForContext("RequestHeaders", httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), destructureObjects: true)
               .ForContext("RequestBody", requestBody)
               .Information("HTTP {RequestMethod} Request sent to {RequestPath}", httpContext.Request.Method, httpContext.Request.Path);
            await next(httpContext);
        }
    }
}
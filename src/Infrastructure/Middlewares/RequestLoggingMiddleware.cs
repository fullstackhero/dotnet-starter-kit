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

            Stream body = httpContext.Request.Body;
            LogContext.PushProperty("RequestTimeUTC", DateTime.UtcNow);
            string requestBody = string.Empty;
            if (httpContext.Request.Path.ToString().Contains("tokens"))
            {
                requestBody = "[Redacted] Contains Sensitive Information.";
            }
            else
            {
                httpContext.Request.EnableBuffering();
                byte[] buffer = new byte[Convert.ToInt32(httpContext.Request.ContentLength)];
                await httpContext.Request.Body.ReadAsync(buffer);
                requestBody = Encoding.UTF8.GetString(buffer).ReplaceWhitespace(string.Empty);
                body.Seek(0, SeekOrigin.Begin);
                httpContext.Request.Body = body;
            }

            LogContext.PushProperty("RequestBody", requestBody);
            Log.ForContext("RequestHeaders", httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), destructureObjects: true)
               .ForContext("RequestBody", requestBody)
               .Information("HTTP {RequestMethod} Request sent to {RequestPath}", httpContext.Request.Method, httpContext.Request.Path);
            await next(httpContext);
        }
    }
}
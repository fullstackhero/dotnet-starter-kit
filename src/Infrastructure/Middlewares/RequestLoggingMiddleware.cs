using DN.WebApi.Application.Abstractions.Services.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DN.WebApi.Infrastructure.Middlewares
{
    public class RequestLoggingMiddleware : IMiddleware
    {
        private readonly ICurrentUser _currentUser;

        private readonly ILogger _logger;

        public RequestLoggingMiddleware(ILoggerFactory loggerFactory, ICurrentUser currentUser)
        {
            _logger = loggerFactory.CreateLogger<RequestLoggingMiddleware>();
            _currentUser = currentUser;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            // Getting the request body is a little tricky because it's a stream
            // So, we need to read the stream and then rewind it back to the beginning
            string requestBody = string.Empty;
            context.Request.EnableBuffering();
            Stream body = context.Request.Body;
            byte[] buffer = new byte[Convert.ToInt32(context.Request.ContentLength)];
            await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            requestBody = Encoding.UTF8.GetString(buffer);
            body.Seek(0, SeekOrigin.Begin);
            context.Request.Body = body;
            if (requestBody != string.Empty)
            {
                requestBody = $"  Body: " + requestBody + Environment.NewLine;
            }

            // Logs should always be secured! However, we will take the extra step of not logging passwords.
            if (context.Request.Path.ToString().Contains("tokens"))
            {
                requestBody = string.Empty;
            }

            try
            {
                await next(context);
            }
            finally
            {
                string user = !string.IsNullOrEmpty(_currentUser.GetUserEmail()) ? _currentUser.GetUserEmail() : "Anonymous";

                LogContext.PushProperty("UserName", user);

                _logger.LogInformation($"{Environment.NewLine}HTTP Request:{Environment.NewLine}" +
                                       $"  Request By: {user}{Environment.NewLine}" +
                                       $"  Tenant: {_currentUser.GetTenantKey() ?? string.Empty}{Environment.NewLine}" +
                                       $"  RemoteIP: {context.Connection.RemoteIpAddress}{Environment.NewLine}" +
                                       $"  Schema: {context.Request.Scheme}{Environment.NewLine}" +
                                       $"  Host: {context.Request.Host}{Environment.NewLine}" +
                                       $"  Method: {context.Request.Method}{Environment.NewLine}" +
                                       $"  Path: {context.Request.Path}{Environment.NewLine}" +
                                       $"  Query String: {context.Request.QueryString}{Environment.NewLine}" +
                                       requestBody +
                                       $"  Response Status Code: {context.Response?.StatusCode}{Environment.NewLine}");
            }
        }
    }
}
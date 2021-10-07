using DN.WebApi.Application.Abstractions.Services.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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
            try
            {
                await next(context);
            }
            finally
            {
                var user = !string.IsNullOrEmpty(_currentUser.GetUserEmail()) ? _currentUser.GetUserEmail() : "Anonymous";
                _logger.LogInformation($"HTTP Request Information:{Environment.NewLine}" +
                                        $"Request By: {user}{Environment.NewLine}" +
                                        $"Tenant: {_currentUser.GetTenantKey() ?? string.Empty}{Environment.NewLine}" +
                                        $"Schema: {context.Request.Scheme}{Environment.NewLine}" +
                                        $"Host: {context.Request.Host}{Environment.NewLine}" +
                                        $"Path: {context.Request.Path}{Environment.NewLine}" +
                                        $"Query String: {context.Request.QueryString}{Environment.NewLine}" +
                                        $"Response Status Code: {context.Response?.StatusCode}");
            }
        }
    }
}
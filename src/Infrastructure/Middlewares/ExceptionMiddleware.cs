using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Wrapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.Identity;
using System.IO;

namespace DN.WebApi.Infrastructure.Middlewares
{
    internal class ExceptionMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly ICurrentUser _currentUser;
        private readonly ISerializerService _jsonSerializer;

        public ExceptionMiddleware(
            ILogger<ExceptionMiddleware> logger,
            ISerializerService jsonSerializer,
            ICurrentUser currentUser)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _currentUser = currentUser;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {

                await next(context);
            }
            catch (Exception exception)
            {
                var response = context.Response;
                response.ContentType = "application/json";
                if (exception is not CustomException && exception.InnerException != null)
                {
                    while (exception.InnerException != null)
                    {
                        exception = exception.InnerException;
                    }
                }

                var user = !string.IsNullOrEmpty(_currentUser.GetUserEmail()) ? _currentUser.GetUserEmail() : "Anonymous";
                _logger.LogError(
                $"{exception.Message}{Environment.NewLine}HTTP Request Information:{Environment.NewLine}" +
                    $"  Request By: {user}{Environment.NewLine}" +
                    $"  Tenant: {_currentUser.GetTenantKey() ?? string.Empty}{Environment.NewLine}" +
                    $"  RemoteIP: {context.Connection.RemoteIpAddress}{Environment.NewLine}" +
                    $"  Schema: {context.Request.Scheme}{Environment.NewLine}" +
                    $"  Host: {context.Request.Host}{Environment.NewLine}" +
                    $"  Path: {context.Request.Path}{Environment.NewLine}" +
                    $"  Query String: {context.Request.QueryString}{Environment.NewLine}" +

                    // $"  Request Body: {context.Request.Body}{Environment.NewLine}" +
                    $"  Response Status Code: {context.Response?.StatusCode}{Environment.NewLine}");

                // TODO: Being able to troubleshoot exceptions almost always requires understanding what
                // the message body contained. The context.Request.Body is a ForwardOnly StreamReader. Middleware, by it's
                // nature, will be making multiple reads against this object. That requires non-trivial handling at the
                // architectural level. This is probably a good starting point: https://markb.uk/asp-net-core-read-raw-request-body-as-string.html

                var responseModel = await ErrorResult<string>.ReturnErrorAsync(exception.Message);
                responseModel.Source = exception.Source;
                responseModel.Exception = exception.Message;

                // try
                // {
                //     var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                //     if (env.ToLower() == "development")
                //     {
                //         _logger.LogError(exception.Message);
                //         responseModel.StackTrace = exception.StackTrace.ToString().Trim().Substring(0, exception.StackTrace.ToString().IndexOf(Environment.NewLine));
                //     }
                // }
                // catch
                // { }
                switch (exception)
                {
                    case CustomException e:
                        response.StatusCode = responseModel.ErrorCode = (int)e.StatusCode;
                        responseModel.Messages = e.ErrorMessages;
                        break;

                    case KeyNotFoundException:
                        response.StatusCode = responseModel.ErrorCode = (int)HttpStatusCode.NotFound;
                        break;

                    default:
                        response.StatusCode = responseModel.ErrorCode = (int)HttpStatusCode.InternalServerError;
                        break;
                }

                string result = string.Empty;
                result = _jsonSerializer.Serialize(responseModel);
                await response.WriteAsync(result);
            }
        }
    }
}
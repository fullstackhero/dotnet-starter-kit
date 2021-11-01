using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Wrapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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

                if (requestBody != string.Empty && context.Request.Path.ToString() != "/api/tokens/")
                {
                    requestBody = $"  Body: " + requestBody + Environment.NewLine;
                }

                // Logs should always be secured! However, we will take the extra step of not logging passwords.
                if (context.Request.Path.ToString() == "/api/tokens/")
                {
                    requestBody = string.Empty;
                }

                var user = !string.IsNullOrEmpty(_currentUser.GetUserEmail()) ? _currentUser.GetUserEmail() : "Anonymous";
                LogContext.PushProperty("UserName", user);

                string errorId = Guid.NewGuid().ToString().Substring(0, 10);
                LogContext.PushProperty("TechSptMsg", "Sorry, an unexpected error has occurred. Provide the following to our technical support department: " + errorId);

                _logger.LogError(
                $"Exception: {exception.Message}{Environment.NewLine}" +
                    $"  Request By: {user}{Environment.NewLine}" +
                    $"  Tenant: {_currentUser.GetTenantKey() ?? string.Empty}{Environment.NewLine}" +
                    $"  RemoteIP: {context.Connection.RemoteIpAddress}{Environment.NewLine}" +
                    $"  Schema: {context.Request.Scheme}{Environment.NewLine}" +
                    $"  Host: {context.Request.Host}{Environment.NewLine}" +
                    $"  Method: {context.Request.Method}{Environment.NewLine}" +
                    $"  Path: {context.Request.Path}{Environment.NewLine}" +
                    $"  Query String: {context.Request.QueryString}{Environment.NewLine}" + requestBody +
                    $"  Response Status Code: {context.Response?.StatusCode}{Environment.NewLine}");

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

                var result = string.Empty;
                result = _jsonSerializer.Serialize(responseModel);
                await response.WriteAsync(result);
            }
        }
    }
}
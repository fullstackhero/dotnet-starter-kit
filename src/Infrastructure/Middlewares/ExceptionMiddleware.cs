using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Wrapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.Middlewares
{
    internal class ExceptionMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly ISerializerService _jsonSerializer;

        public ExceptionMiddleware(
            ILogger<ExceptionMiddleware> logger,
            ISerializerService jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
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

                _logger.LogError(exception.Message);
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
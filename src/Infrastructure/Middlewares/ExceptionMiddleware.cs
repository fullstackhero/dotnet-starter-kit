using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Application.Exceptions;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;

namespace DN.WebApi.Infrastructure.Middlewares
{
    internal class ExceptionMiddleware : IMiddleware
    {
        private readonly ICurrentUser _currentUser;
        private readonly ISerializerService _jsonSerializer;
        private readonly IStringLocalizer<ExceptionMiddleware> _localizer;

        public ExceptionMiddleware(
            ISerializerService jsonSerializer,
            ICurrentUser currentUser,
            IStringLocalizer<ExceptionMiddleware> localizer)
        {
            _jsonSerializer = jsonSerializer;
            _currentUser = currentUser;
            _localizer = localizer;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                string email = !string.IsNullOrEmpty(_currentUser.GetUserEmail()) ? _currentUser.GetUserEmail() : "Anonymous";
                var userId = _currentUser.GetUserId();
                string tenant = _currentUser.GetTenantKey() ?? string.Empty;
                if (userId != Guid.Empty) LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("UserEmail", email);
                if (!string.IsNullOrEmpty(tenant)) LogContext.PushProperty("Tenant", tenant);
                string errorId = Guid.NewGuid().ToString();
                LogContext.PushProperty("ErrorId", errorId);
                LogContext.PushProperty("StackTrace", exception.StackTrace);
                var responseModel = await ErrorResult<string>.ReturnErrorAsync(exception.Message);
                responseModel.Source = exception.TargetSite.DeclaringType.FullName;
                responseModel.Exception = exception.Message;
                responseModel.ErrorId = errorId;
                responseModel.SupportMessage = _localizer["exceptionmiddleware.supportmessage"];
                var response = context.Response;
                response.ContentType = "application/json";
                if (exception is not CustomException && exception.InnerException != null)
                {
                    while (exception.InnerException != null)
                    {
                        exception = exception.InnerException;
                    }
                }

                switch (exception)
                {
                    case CustomException e:
                        response.StatusCode = responseModel.StatusCode = (int)e.StatusCode;
                        responseModel.Messages = e.ErrorMessages;
                        break;

                    case KeyNotFoundException:
                        response.StatusCode = responseModel.StatusCode = (int)HttpStatusCode.NotFound;
                        break;

                    default:
                        response.StatusCode = responseModel.StatusCode = (int)HttpStatusCode.InternalServerError;
                        break;
                }

                Log.Error($"{responseModel.Exception} Request failed with Status Code {context.Response.StatusCode} and Error Id {errorId}.");
                await response.WriteAsync(_jsonSerializer.Serialize(responseModel));
            }
        }
    }
}
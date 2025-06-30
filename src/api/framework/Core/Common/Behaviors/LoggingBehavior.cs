using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Core.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid();

        try
        {
            _logger.LogInformation(
                "[{RequestId}] Starting to handle {RequestName}. Request: {@Request}",
                requestId,
                requestName,
                request);

            var response = await next().ConfigureAwait(false);

            _logger.LogInformation(
                "[{RequestId}] Successfully handled {RequestName}. Response: {@Response}",
                requestId,
                requestName,
                response);

            return response;
        }
        catch (FshException ex)
        {
            _logger.LogError(
                ex,
                "[{RequestId}] Business error occurred while handling {RequestName}. Request: {@Request}",
                requestId,
                requestName,
                request);

            throw;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Unexpected error occurred while handling {requestName}";
            _logger.LogError(
                ex,
                "[{RequestId}] {ErrorMessage}. Request: {@Request}",
                requestId,
                errorMessage,
                request);

            throw new FshException(errorMessage, ex);
        }
    }
}
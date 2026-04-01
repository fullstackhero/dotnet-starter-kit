using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetAuditById;
using FSH.Modules.Auditing.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FSH.Modules.Auditing.Features.v1.GetAuditById;

public sealed class GetAuditByIdQueryHandler(AuditDbContext dbContext, ILogger<GetAuditByIdQueryHandler> logger) : IQueryHandler<GetAuditByIdQuery, AuditDetailDto>
{
    public async ValueTask<AuditDetailDto> Handle(GetAuditByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var record = await dbContext.AuditRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken)
            .ConfigureAwait(false) ?? throw new KeyNotFoundException($"Audit record {query.Id} not found.");
        JsonElement payload;
        try
        {
            using var document = JsonDocument.Parse(record.PayloadJson);
            payload = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse audit payload JSON for record {AuditId}.", query.Id);
            payload = JsonDocument.Parse("{}").RootElement.Clone();
        }

        return new AuditDetailDto
        {
            Id = record.Id,
            OccurredAtUtc = record.OccurredAtUtc,
            ReceivedAtUtc = record.ReceivedAtUtc,
            EventType = (AuditEventType)record.EventType,
            Severity = (AuditSeverity)record.Severity,
            TenantId = record.TenantId,
            UserId = record.UserId,
            UserName = record.UserName,
            TraceId = record.TraceId,
            SpanId = record.SpanId,
            CorrelationId = record.CorrelationId,
            RequestId = record.RequestId,
            Source = record.Source,
            Tags = (AuditTag)record.Tags,
            Payload = payload
        };
    }
}
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Webhooks.Contracts.Dtos;
using FSH.Modules.Webhooks.Contracts.v1.GetWebhookDeliveries;
using FSH.Modules.Webhooks.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Webhooks.Features.v1.GetWebhookDeliveries;

public sealed class GetWebhookDeliveriesQueryHandler(
    WebhookDbContext dbContext) : IQueryHandler<GetWebhookDeliveriesQuery, PagedResponse<WebhookDeliveryDto>>
{
    public async ValueTask<PagedResponse<WebhookDeliveryDto>> Handle(
        GetWebhookDeliveriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dbQuery = dbContext.Deliveries
            .AsNoTracking()
            .Where(d => d.SubscriptionId == query.SubscriptionId)
            .OrderByDescending(d => d.AttemptedAtUtc);

        var totalCount = await dbQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await dbQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new WebhookDeliveryDto
            {
                Id = d.Id,
                SubscriptionId = d.SubscriptionId,
                EventType = d.EventType,
                HttpStatusCode = d.HttpStatusCode,
                Success = d.Success,
                AttemptCount = d.AttemptCount,
                AttemptedAtUtc = d.AttemptedAtUtc,
                ErrorMessage = d.ErrorMessage
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<WebhookDeliveryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }
}

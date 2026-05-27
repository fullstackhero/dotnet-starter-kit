using FSH.Framework.Shared.Persistence;
using FSH.Modules.Webhooks.Contracts.Dtos;
using FSH.Modules.Webhooks.Contracts.v1.GetWebhookSubscriptions;
using FSH.Modules.Webhooks.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Webhooks.Features.v1.GetWebhookSubscriptions;

public sealed class GetWebhookSubscriptionsQueryHandler(
    WebhookDbContext dbContext) : IQueryHandler<GetWebhookSubscriptionsQuery, PagedResponse<WebhookSubscriptionDto>>
{
    public async ValueTask<PagedResponse<WebhookSubscriptionDto>> Handle(
        GetWebhookSubscriptionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dbQuery = dbContext.Subscriptions
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAtUtc);

        var totalCount = await dbQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await dbQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new WebhookSubscriptionDto
            {
                Id = s.Id,
                Url = s.Url,
                Events = s.EventsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                IsActive = s.IsActive,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<WebhookSubscriptionDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }
}

using FSH.Framework.Shared.Persistence;
using FSH.Modules.Webhooks.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Webhooks.Contracts.v1.GetWebhookSubscriptions;

public sealed record GetWebhookSubscriptionsQuery(int PageNumber = 1, int PageSize = 10)
    : IQuery<PagedResponse<WebhookSubscriptionDto>>;

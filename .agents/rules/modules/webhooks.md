# Module: Webhooks

Tenant-scoped outbound webhook subscriptions with HMAC-signed delivery and retries. Module `Order = 400`.

**Entities / DbContext:** `WebhookSubscription` (`Url`, `EventsCsv`, `SecretHash`, `IsActive`), `WebhookDelivery` (per-attempt log). `WebhookDbContext` (tenant-filtered). Contracts expose **DTOs only** — `IWebhookDispatcher`/`IWebhookDeliveryService` are internal.
**Areas:** Create/Delete/Get subscriptions, GetDeliveries, Test. Full list: `Features/v1/` or `/scalar`.

## Gotchas

- **Fan-out is an open-generic handler** — `WebhookFanoutHandler<TEvent>` is registered as an open generic, so it handles **every** `IIntegrationEvent` with no per-event wiring. It skips events with null `TenantId` (subscriptions are tenant-only) and matches event-type name against each subscription's `EventsCsv` (`*` wildcard supported).
- **Restore tenant context in the background** — both the fan-out handler and `WebhookDispatchJob` set `IMultiTenantContext` in a fresh scope before reading the tenant-filtered DbContext (background pumps/Hangfire carry no HTTP context). This is the canonical pattern for any background reader of tenant data — see `eventing.md`, `jobs.md`.
- **HMAC signing** — `X-Webhook-Signature: sha256=<hex HMACSHA256>` (`WebhookPayloadSigner.Sign`), plus `X-Webhook-Event` and `X-Webhook-Delivery-Id` headers.
- **Delivery** — `WebhookDispatcher.EnqueueAsync` enqueues a Hangfire `WebhookDispatchJob` per subscription; `[AutomaticRetry(Attempts=4, DelaysInSeconds={30,120,600,3600})]`. Transient (5xx/408/429) throws to reschedule; permanent 4xx completes silently. Each attempt persists a `WebhookDelivery` row. The `"Webhooks"` HttpClient uses `AddHeroResilience` (see `resilience.md`).

# HTTP resilience

`src/BuildingBlocks/Web/HttpResilience/`. Uses `Microsoft.Extensions.Http.Resilience` (Polly v8).

## Pattern — opt-in per HttpClient

`AddHeroResilience(config)` is an `IHttpClientBuilder` extension that adds `AddStandardResilienceHandler` configured from `HttpResilienceOptions` (retry, total-request timeout, attempt timeout, circuit breaker). It is **NOT global** — chain it onto the specific outbound client that needs it:

```csharp
builder.Services.AddHttpClient("Webhooks", ...)
    .AddHeroResilience(builder.Configuration);
```

Defaults (`HttpResilienceOptions`): 3 retries, 30s total, 10s per attempt, 50% failure ratio, throughput 10. No-ops when `Enabled=false`.

## Notes

- Only outbound integrations need this. The only current caller is the Webhooks delivery client (`WebhooksModule.cs`). Add it to any new typed/named `HttpClient` that calls a flaky external service.
- For internal ret/timeout of *background* work, prefer Hangfire's `[AutomaticRetry]` on the job (see `modules/webhooks.md`) — that's durable across restarts; the resilience handler only covers the in-flight HTTP call.

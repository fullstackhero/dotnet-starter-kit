# Module: Billing

Plans, subscriptions, usage metering, monthly invoicing. **Manual payment marking — no payment provider.** Module `Order = 500`.

**Entities / DbContext:** `BillingPlan`, `Subscription`, `Invoice` (+ `InvoiceLineItem`), `UsageSnapshot`. **`BillingDbContext : DbContext`** (NOT `BaseDbContext`) — billing lives in the main DB with an explicit `TenantId` column for cross-tenant admin visibility, filtered in query services. Contracts = DTOs; `IBillingService`/`IUsageReporter` are internal.
**Areas:** Plans, Subscriptions, Invoices (generate/issue/mark-paid/void), Usage (capture/get). Monthly invoice job (`5 0 1 * *`). Full list: `Features/v1/` or `/scalar`.

## Gotchas

- **`BillingPlan` is `IGlobalEntity`** — platform-wide catalogue rows, **not tenant-scoped** (opts out of tenant isolation). A plan's `Key` matches the quota config key (e.g. `"pro"`): limits come from `QuotaOptions`, prices/overage from the plan.
- **`BillingDbContext` is a plain `DbContext`** — tenant filtering is done explicitly in query services, not by the `BaseDbContext` auto-filter. Don't assume the global tenant filter applies here.
- **Invoice state machine** — `Draft → Issued → Paid | Void`. Line items only addable in Draft; a Paid invoice can't be voided; totals recompute on add; Issue defaults due = +14 days.
- **Usage metering is idempotent** — `IUsageReporter.CaptureForPeriodAsync` reads `IQuotaService` and persists one `UsageSnapshot` per `QuotaResource` per (tenant, period), so invoicing math is reproducible even after a mid-period plan change.

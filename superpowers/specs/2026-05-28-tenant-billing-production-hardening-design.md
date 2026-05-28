# Tenant Billing — Production Hardening & Completion (Phases 3–4 + Dashboard + Tests)

**Date:** 2026-05-28
**Branch:** `feat/tenant-activation-confirm` (current) → new work branch `feat/tenant-billing-production`
**Status:** Approved (user directed autonomous implementation)
**Builds on:** `superpowers/specs/2026-05-28-tenant-billing-lifecycle-design.md` (the original lifecycle spec — read it first; this spec is the completion/hardening delta).

## Problem

The tenant billing lifecycle (the SaaS product's headline feature) shipped Phase 1 (core backend) and
Phase 2 (admin UI) via PR #1269. It is **not yet complete or fully production-grade**:

- Phase 3 (expiry/renewal **notifications**) and Phase 4 (**PDF invoices**) were deferred.
- The **tenant-facing dashboard** has only a read-only invoice list + a usage widget — no plan/validity
  view, no expiry/grace warnings, no invoice detail, no PDF, and a `SubscriptionStatus` enum that
  doesn't match the backend.
- One real **bug**: `TenantService.RenewAsync` uses `DateTime.UtcNow` instead of the injected
  `TimeProvider` (clock not controllable; inconsistent with its own handler).
- **Edge-case test gaps**: event-handler idempotency, grace-window boundaries, double-renew /
  renew-in-grace, plan seeding, single-active-subscription invariant, invoice state-machine guards at
  the endpoint, usage-snapshot uniqueness.

This spec completes the feature and hardens it. **Payment model stays invoice-as-record (no gateway);
no proration; dashboard is view + warnings (operator still renews)** — confirmed with the user.

## Locked decisions (this pass)

1. **Invoice-as-record, no payment gateway** — unchanged. Access is gated by `ValidUpto` + grace, not by
   payment.
2. **No proration** — renew/change-plan stacks a full term and issues a full-term invoice.
3. **Dashboard = view + warnings**; renewal/plan-change remains an operator action in admin.
4. **PDF engine = QuestPDF**, isolated behind `IInvoicePdfRenderer`. The QuestPDF Community license is
   free only for orgs < $1M USD/yr revenue — **documented** in the docs repo + changelog; swappable
   because it sits behind the interface.
5. **Include `AdjustTenantValidityCommand`** — operator validity override for comps/support (no invoice,
   no event, audit-noted).
6. **Notifications push = email** (`IMailService`, hand-built HTML). The dashboard banner is the pull
   side. No cross-resolving Identity user IDs for in-app expiry toasts in this pass.

## Workstream A — Backend hardening

### A1. Clock fix (`TenantService.RenewAsync`)
`src/Modules/Multitenancy/Modules.Multitenancy/Services/TenantService.cs:203` — replace
`var now = DateTime.UtcNow;` with `var now = _timeProvider.GetUtcNow().UtcDateTime;`. The
`TimeProvider` is already injected (line 26/45). No behavior change in prod; makes the stacking logic
clock-controllable for tests and consistent with `GetStatusAsync` / the handler.

### A2. `X-Subscription-Grace` response header
`MultitenancyModule` post-auth guard (the existing expiry block): when a non-root tenant is **past
`ValidUpto` but within grace**, add response header `X-Subscription-Grace: <daysLeft>` (integer,
ceil of remaining days). Hard-block (403) past grace stays as-is. Active tenants get no header. This is
a cheap signal for clients; the dashboard banner does not depend on it (it uses `tenants/me/status`).

### A3. Indexes — ALREADY DONE (verified during planning; audit was wrong)
Reading the EF configs shows both indexes the audit claimed were missing already exist:
- `UsageSnapshotConfiguration` has a **unique** index `ux_usage_snapshots_tenant_period_resource` on
  `(TenantId, PeriodYear, PeriodMonth, Resource)`.
- `SubscriptionConfiguration` has `(TenantId, Status)` **and** a **partial-unique**
  `ux_subscriptions_tenantid_active` (`TenantId` WHERE `Status = Active`) — the single-active invariant
  is already a DB constraint.

**No index migration needed.** The replace flow (cancel-old → create-new in one `SaveChanges`) works
against the partial-unique index in prod (EF emits the UPDATE-to-Cancelled before the INSERT). E still
adds an integration test asserting the invariant + that the unique-active index rejects a forced second
active row, to lock the behavior against regressions.

### A4. `AdjustTenantValidityCommand` (operator override)
- Contract: `Modules.Multitenancy.Contracts/v1/AdjustTenantValidity/AdjustTenantValidityCommand.cs`
  `record AdjustTenantValidityCommand(string TenantId, DateTime ValidUpto) : ICommand<...Response>`.
- Handler: load tenant, `tenant.SetValidity(validUpto)` (forward-only — see
  `[[project_apptenantinfo_setvalidity_forward_only]]`; if a backdate is required, set the property
  directly with a guard + log), `UpdateAsync`, refresh cache. **No** event, **no** invoice.
- Validator: `TenantId` NotEmpty; `ValidUpto` must be UTC-specifiable.
- Endpoint: `POST /api/v1/tenants/{id}/adjust-validity`, root-operator permission only (mirror the
  RenewTenant permission/guard).
- Admin UI action (Workstream D-admin): a small "Adjust validity" action on the tenant detail page.

## Workstream B — Phase 3: Notifications

### B1. New integration events
In `Modules.Multitenancy.Contracts/Events/` (tenant lifecycle owns these; the scan job lives in
Multitenancy):
- `TenantNearingExpiryIntegrationEvent` — `+ int DaysRemaining`.
- `TenantEnteredGraceIntegrationEvent`.
- `TenantExpiredIntegrationEvent`.

Common fields (mirror `TenantSubscribedIntegrationEvent`): `Id, OccurredOnUtc, TenantId, CorrelationId,
Source, TenantName, AdminEmail, PlanKey, ValidUpto, GraceEndsUtc`.

In `Modules.Billing.Contracts/Events/`:
- `InvoiceIssuedIntegrationEvent` — `Id, OccurredOnUtc, TenantId, CorrelationId, Source, InvoiceId,
  InvoiceNumber, decimal Amount, string Currency, DateTime? DueAtUtc, int PeriodYear, int PeriodMonth`.

### B2. `TenantExpiryScanJob` (Multitenancy)
- `src/Modules/Multitenancy/Modules.Multitenancy/Services/TenantExpiryScanJob.cs`, `RunAsync(CancellationToken)`.
- Injects `IMultiTenantStore<AppTenantInfo>`, `IEventBus`, `IOptions<TenantBillingOptions>`,
  `TimeProvider`, `ILogger`.
- Daily recurring job registered in `MultitenancyModule.MapEndpoints` via `IRecurringJobManager.AddOrUpdate`
  (`"0 2 * * *"`, `TimeZoneInfo.Utc`, id `"tenant-expiry-scan"`).
- For each **active, non-root** tenant, compute state from `now` vs `ValidUpto` and
  `graceEnds = ValidUpto + GraceWindowDays`:
  - `now > graceEnds` → publish `TenantExpired`.
  - `ValidUpto < now <= graceEnds` → publish `TenantEnteredGrace`.
  - `ValidUpto - ExpiryNotificationLeadDays <= now <= ValidUpto` → publish `TenantNearingExpiry`
    (`DaysRemaining = ceil((ValidUpto - now).TotalDays)`).
  - else → no event.
- Per-tenant try/catch (one failure doesn't block others), structured logging. Publishes via `IEventBus`
  (direct, in-memory) — matching the create/renew pattern, **not** the Outbox.

### B3. Dedup ownership + Notifications email handlers
**Where dedup lives (decided after self-review):** the scan job fires **daily**, so a fresh event Id
each day defeats inbox dedup → without a ledger the tenant gets a grace/expired email every day. The
ledger must therefore be keyed by `(tenant, state, ValidUpto)`, not by event Id. It **cannot** live in
`NotificationsDbContext`: that's a tenant-filtered `BaseDbContext`, but the expiry events are published
from a background job with **no tenant context**, and Notifications may not reference Multitenancy's
runtime to borrow a cross-tenant context (module boundary). So:

- **Dedup is owned by the scan job**, in a new `TenantExpiryNotice` entity in Multitenancy's
  `TenantDbContext` (which is Finbuckle's `EFCoreStoreDbContext<AppTenantInfo>` — already cross-tenant,
  **not** tenant-filtered, so no tenant-context dance and not subject to the `BaseDbContext`
  tenant-isolation rule). Fields: `Id, TenantId, NoticeType, ValidUptoUtc, CreatedAtUtc`; **unique**
  index `(TenantId, NoticeType, ValidUptoUtc)`. The job does check-or-insert **before** publishing, so a
  given `(tenant, state, validity-period)` is published exactly once; renewal moves `ValidUpto` and
  re-arms all states. Migration in the PostgreSQL project under the tenant/Multitenancy folder.
- **Notifications handlers stay pure senders.** New `sealed` `IIntegrationEventHandler<T>` in
  `Modules.Notifications/IntegrationEventHandlers/` for each of the 4 events (3 expiry + `InvoiceIssued`),
  auto-registered by the existing `AddIntegrationEventHandlers(typeof(NotificationsModule).Assembly)`
  scan. Each builds a `MailRequest` (subject + hand-built HTML body via a private `EmailBodies` helper)
  and `await _mailService.SendAsync(...)` inside try/catch + warn-log (email failure must not throw —
  matches `UserRegisteredEmailHandler`). The in-memory bus inbox dedups same-event-Id redelivery.
- **`InvoiceIssued`** needs no ledger — it's one-shot per invoice issue, and inbox dedups redelivery.

**Tradeoff (stated explicitly):** push email is **best-effort once per state per validity period** — a
transient send failure is logged but not re-attempted until `ValidUpto` changes. This is acceptable
because the **dashboard banner (Workstream D) is the always-accurate pull side**: a tenant in grace
always sees the banner on load regardless of email delivery. Push + pull together cover the case.

### B4. Config
`TenantBillingOptions.ExpiryNotificationLeadDays` (default 7), bound from the existing `"Billing"`
section. Document the key.

## Workstream C — Phase 4: PDF invoices

- Add `QuestPDF` NuGet to `Modules.Billing` (pin a current version; `QuestPDF.Settings.License =
  LicenseType.Community` set once at module init).
- `IInvoicePdfRenderer` (Billing service) — `byte[] Render(InvoiceDto invoice)`. Implementation
  `InvoicePdfRenderer` builds a clean A4 invoice: header (invoice #, status, dates), bill-to (tenant),
  period, line-items table (kind, description, qty, unit price, amount), subtotal, notes. Currency-aware
  formatting; decimals match the stored `18,4` → display 2dp.
- Endpoints (mirror the existing permission split):
  - `GET /api/v1/billing/invoices/{id}/pdf` — operator (Billing.View), any tenant's invoice.
  - `GET /api/v1/billing/invoices/me/{id}/pdf` — tenant-self, only the caller's own invoice (404 on
    cross-tenant, no leak — mirror `GetInvoiceById`/`GetMyInvoices`).
  - Both return `FileContentResult`/`Results.File(bytes, "application/pdf", "{invoiceNumber}.pdf")`.
- On-demand only (no stored artifact); persisting to the Files module is a future option.

## Workstream D — Dashboard self-serve (view + warnings) + admin glue

### D-dashboard (`clients/dashboard`)
- **`GET /api/v1/tenants/me/status`** (new tenant-self endpoint in Multitenancy): returns a trimmed
  `TenantStatusDto` for the **calling** tenant (resolved from the tenant context, not a route id) —
  `plan, validUpto, expiryState, graceEndsUtc`. Permission: any authenticated tenant user.
- **Subscription page** at `/subscription` (the existing `/invoices` list stays; this is the
  plan/usage view): current plan, validity date, expiry badge (Active/InGrace/Expired), usage snapshots
  with limit + overage, recent invoices (link to detail). Reuses the dashboard's existing card/table
  primitives. Add a nav entry.
- **Global expiry/grace banner** in the dashboard `AppShell`: query `tenants/me/status` (staleTime ~5m);
  show a warning bar when `InGrace` ("subscription expired — N days of grace left, contact your
  operator") or when `Active` within `ExpiryNotificationLeadDays` ("expires in N days"). Dismissible per
  session; reappears on reload while the condition holds.
- **Invoice detail page** (new): line items, totals, status, dates, **Download PDF** button
  (`invoices/me/{id}/pdf`).
- Fix `clients/dashboard/src/api/billing.ts` `SubscriptionStatus` to match backend
  (`Active`/`Suspended`/`Cancelled`); align invoice line-item typing (currently `unknown[]`).
- Register lazy routes; the dashboard has no per-route permission system (tenant-scoped), so guarding is
  by auth only.

### D-admin (`clients/admin`) glue
- **Download PDF** button on the admin invoice detail page (`invoices/{id}/pdf`).
- **Adjust validity** action on the tenant detail page (calls A4 endpoint) — small dialog with a date
  picker; root-operator only (mirror the Renew action's permission gate).
- Plan-form validation: enforce non-negative `monthlyBasePrice`/`annualPrice`/overage rates client-side
  (currently relies on server rejection).

## Workstream E — Tests

### Unit (`Billing.Tests`, `Multitenancy.Tests`)
- `GetStatusAsync` expiry-state boundaries: `now == ValidUpto` (Active), `now == graceEnds` (InGrace),
  `now == graceEnds + 1s` (Expired), `now == ValidUpto + 1s` (InGrace). Drive via `FakeTimeProvider`.
- `TenantExpiryScanJob` state selection: nearing / grace / expired / no-event, including boundaries;
  root + inactive tenants excluded. (Use `Substitute.For<IEventBus>()`, assert exact event type +
  `Received(1)`.)
- Email body builders produce expected subject + key fields (smoke).
- `InvoicePdfRenderer.Render` returns a non-empty `%PDF`-prefixed byte[] for a representative invoice.

### Integration (`Integration.Tests`, Testcontainers)
- **Event-handler idempotency:** publish `TenantSubscribed` twice (same `Id`) → exactly one subscription
  + one subscription invoice (inbox dedup) — and a *distinct* redelivery via the invoice-number guard.
- **Grace boundaries (middleware + login):** request/login at `ValidUpto` (allowed), within grace
  (allowed, `X-Subscription-Grace` present), at `graceEnds` (allowed), `graceEnds + 1s` (403/401).
- **Renewal:** double-renew stacks (≈ 2 terms from now); renew while in grace (starts from `now`, since
  `ValidUpto < now`); renew with a different plan key swaps the active subscription + issues a new
  subscription invoice + `PlanChanged=true`; renew when `ValidUpto` is in the future stacks from
  `ValidUpto`.
- **Plan seeding:** `BillingDbInitializer` seeds `free`/`pro`/`pro-annual` with correct interval/price
  on an empty plans table; idempotent (no duplication on re-run).
- **Single-active-subscription invariant:** after a plan change, exactly one `Active` subscription for
  the tenant; the prior is `Cancelled` with `EndUtc == new.StartUtc`.
- **Invoice state-machine at the endpoint:** `Void → mark-paid` rejected (409/400); `Paid → void`
  rejected; issue twice idempotent/rejected as designed.
- **Usage-snapshot uniqueness:** second capture for the same `(tenant, period, resource)` does not
  duplicate (app idempotency) and the DB unique index rejects a forced duplicate insert.
- **`TenantExpiryScanJob` end-to-end:** seed tenants in each state, run job, assert correct events
  emitted; run twice → dedup ledger prevents a second email per state/period.
- **`InvoiceIssued` → email:** subscription invoice issue triggers the Notifications handler (assert via
  a captured/fake `IMailService`).
- **PDF endpoints:** operator gets 200 `application/pdf` for any invoice; tenant-self gets 200 for own,
  404 for another tenant's (no leak).
- **`tenants/me/status`:** returns the caller's plan/expiryState/grace; 401 unauthenticated.
- **`AdjustTenantValidity`:** sets `ValidUpto`, no invoice/subscription created, root-only (403 for
  tenant admin), validity reflected in `GetStatus`.

### Architecture (`Architecture.Tests`)
- `TenantExpiryNotice` lives in `TenantDbContext` (Finbuckle `EFCoreStoreDbContext`, **not** a
  `BaseDbContext`), so the tenant-isolation NetArchTest (which targets `BaseDbContext`-derived contexts)
  does not apply — confirm it isn't flagged. No new tenant-isolated entity is introduced in this pass.

### Frontend (Playwright, route-mocked) — `clients/dashboard` + `clients/admin`
- Dashboard: subscription page renders plan/usage/invoices; expiry banner shows for InGrace and
  nearing-expiry mocks; invoice detail renders line items + triggers the PDF download request.
- Admin: PDF button on invoice detail issues the `/pdf` request; Adjust-validity dialog posts the
  command; plan-form rejects negative prices client-side.

## Module wiring checklist

- **No new module** ⇒ no change to the four Mediator/`moduleAssemblies` wire points.
- Billing: `IInvoicePdfRenderer` DI registration; QuestPDF license init; publish `InvoiceIssued` from the
  two subscription-invoice handlers; new PDF endpoints in `MapEndpoints`.
- Multitenancy: register `TenantExpiryScanJob` recurring job in `MapEndpoints`; new events in Contracts;
  `tenants/me/status` + `adjust-validity` endpoints/handlers/validators; bind
  `ExpiryNotificationLeadDays`.
- Notifications: new handlers (auto-scanned). No DbContext change (dedup lives in Multitenancy).
  Ensure `Modules.Notifications` references `Modules.Multitenancy.Contracts` + `Modules.Billing.Contracts`
  for the new event types (add the refs if missing).
- Multitenancy: `TenantExpiryNotice` entity + EF config in `TenantDbContext`.
- Migrations: one Billing migration (indexes), one Multitenancy/tenant migration (`TenantExpiryNotice`).
  DbMigrator picks them up; run `apply` in dev. Full-build before each `migrations add` (snapshot footgun).

## Error handling

- Email send failure → caught + warn-logged in the handler; never throws (don't break the scan job /
  invoice issue). The dedup row is written by the scan job **before** publishing (to prevent daily
  spam), so a transient send failure is **not** auto-retried within the same validity period — the
  always-accurate dashboard banner (D) is the fallback for the logged-in tenant; an operator can re-emit
  by clearing the `TenantExpiryNotice` row if ever needed.
- PDF render failure → 500 via the global handler; no partial file. Cross-tenant PDF → 404 (no leak).
- `AdjustTenantValidity` backdating → `SetValidity` is forward-only; if a backdate is explicitly needed,
  set the property directly behind a guard + audit log (documented in the handler).
- Scan job: per-tenant try/catch; job-level failure surfaces in the Hangfire dashboard + retry.

## Out of scope (unchanged)

Payment gateway, self-service checkout/renewal, auto-renew, mid-term proration, multi-currency
conversion, dunning beyond the single grace window, in-app (SignalR) expiry toasts resolved to Identity
user IDs (dashboard banner covers the logged-in case via pull).

## Docs (golden rule #10)

Update the separate docs repo (`github.com/fullstackhero/docs`) + changelog once behavior lands:
new config key `Billing:ExpiryNotificationLeadDays`; new endpoints (`tenants/me/status`,
`tenants/{id}/adjust-validity`, invoice `/pdf` x2); the **QuestPDF license note** for downstream
commercial users; the dashboard subscription page + expiry banner; expiry/renewal emails.

## Phasing (implementation order)

1. **A** — backend hardening (clock fix, header, indexes migration, AdjustTenantValidity) + its tests.
2. **B** — notifications (events, scan job, handlers, ledger, config) + tests.
3. **C** — PDF (renderer, endpoints) + tests.
4. **D** — dashboard + admin glue (status endpoint first, then UI) + Playwright tests.
5. **E** — fill any remaining cross-cutting integration/regression tests; full build + `dotnet test`.

Each phase: build clean (`TreatWarningsAsErrors`) and green tests before the next.

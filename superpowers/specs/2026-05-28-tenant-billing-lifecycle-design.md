# Tenant Billing Lifecycle — Design Spec

**Date:** 2026-05-28
**Branch:** `feat/tenant-billing-lifecycle`
**Status:** Approved (user directed autonomous implementation)

## Problem

The Billing module (`BillingPlan`, `Subscription`, `Invoice`, `UsageSnapshot`) and the
Multitenancy module (`AppTenantInfo` with `ValidUpto` / `IsActive` / `Plan`) are fully built but
**not wired together**. Creating a tenant never touches Billing: no `Subscription` row, no `Invoice`,
and `ValidUpto` is hardcoded to *now + 1 month* regardless of plan. There is no plan-driven renewal
and expiry is only enforced at login (an already-issued JWT keeps working past expiry).

This feature makes the tenant lifecycle **drive** billing (create/renew → subscription + invoice +
validity) and makes billing/expiry **gate** access (middleware enforcement with a grace window).

## Decisions (locked)

1. **Activation model:** Invoice-as-record, no payment gateway. Operator creates tenant + picks a
   plan; the system generates an invoice and sets `ValidUpto` from the plan term immediately. Payment
   is tracked manually (mark-paid). Access is gated by `ValidUpto`, **not** by whether the invoice is
   paid — the invoice is bookkeeping.
2. **Term model:** A plan carries a `BillingInterval` (`Monthly` | `Yearly`). `ValidUpto` advances by
   the interval; the invoice charges that interval's price.
3. **Renewal trigger:** Manual operator action (plan-driven). Replaces today's explicit-date
   `UpgradeTenant`.
4. **Expiry behavior:** Block everywhere (middleware guard, not just login) **plus** a configurable
   grace window past `ValidUpto` during which requests still succeed; hard-block after grace.
5. **In scope (all four):** change plan/tier at renew; mandatory plan + trial fallback; expiry/renewal
   notifications; PDF/downloadable invoices.
6. **Wiring:** Approach 1 — event-driven side-effects + a thin synchronous plan-term read. Multitenancy
   stays the authority over the tenant store; Billing reacts to integration events via Outbox/Inbox.

## Module boundary & dependency edges

Cross-module talk only via `.Contracts` (golden rule #1). This feature introduces two new edges, both
through Contracts (no runtime→runtime; no project cycle since Contracts depend only on `Shared`):

- `Modules.Multitenancy` (runtime) → `Modules.Billing.Contracts` — to dispatch a read-only
  `GetPlanTermQuery` via Mediator when computing `ValidUpto`.
- `Modules.Billing` (runtime) → `Modules.Multitenancy.Contracts` — to handle the
  `TenantSubscribed` / `TenantRenewed` integration events Multitenancy publishes.

Integration events are the repo's dominant cross-module pattern (Notifications already consumes Chat /
Identity events). Publish via the **Outbox** only (`eventing.md`); handlers are `sealed`
`IIntegrationEventHandler<T>`, idempotent via the Inbox.

## Data model changes

### `BillingPlan` (Billing) — extend, in-module (NOT BuildingBlocks)

- Add `BillingInterval Interval` (enum `Monthly = 0`, `Yearly = 1`), default `Monthly`.
- Add `decimal? AnnualPrice` — meaningful only when `Interval == Yearly`; `null` ⇒ yearly term price =
  `12 × MonthlyBasePrice`. `MonthlyBasePrice` remains the canonical monthly rate (used by the overage
  flow and by monthly-interval plans).
- Add helpers:
  - `decimal GetTermPrice()` → `Interval == Yearly ? (AnnualPrice ?? 12 × MonthlyBasePrice) : MonthlyBasePrice`.
  - `int GetTermMonths()` → `Interval == Yearly ? 12 : 1`.
- Update `Create()` / `Update()` signatures to accept `interval` + `annualPrice`. Migration required.

### `Invoice` (Billing) — extend

- Add `InvoicePurpose Purpose` (enum `Subscription = 0`, `Usage = 1`), default `Usage` (preserves
  existing rows' meaning).
- Add nullable `DateTime? PeriodStartUtc` / `DateTime? PeriodEndUtc` — the term span for `Subscription`
  invoices. The existing `PeriodYear` / `PeriodMonth` remain (the anchor month + the usage flow's key).
- `CreateDraft` gains an overload (or extra args) carrying `purpose` + optional period span.
- Invoice number scheme becomes purpose-aware so the two streams never collide on idempotency keys:
  - `Subscription` → `SUB-{yyyymm}-{tenant8}`
  - `Usage` → `USG-{yyyymm}-{tenant8}` (renamed from today's `INV-…`)
- Migration required (new columns; backfill `Purpose = Usage` for existing rows).

### `Subscription` (Billing) — no structural change

`TenantId, PlanId, StartUtc, EndUtc, Status` already exist. Create/renew set `EndUtc` = term end;
plan change = cancel current + create new (matching today's `AssignSubscription`).

### `AppTenantInfo` (Multitenancy) — **no change**

`Plan`, `ValidUpto`, `IsActive`, `AddValidity(months)`, `SetValidity(date)`, `Activate()`,
`Deactivate()` already exist. `tenant.Plan = plan.Key` (drives quotas) stays in sync with the active
`Subscription.PlanId`. **No `BuildingBlocks` edit** — stays clear of the protected zone.

## The reconciliation: term billing vs. the monthly job

Today `BillingService.GenerateInvoiceForPeriodAsync` (monthly Hangfire job, cron `5 0 1 * *`) bills
**base fee + overages** per (tenant, year, month) as a Draft invoice. Adding a term base-fee invoice
at create/renew would double-bill monthly plans and not fit yearly plans. Resolution:

- **`Subscription` invoice** = plan term base fee, generated at **tenant create** and **each
  renew/change-plan**, covering `[ValidUpto_old, ValidUpto_new]`, amount = `plan.GetTermPrice()`.
- **`Usage` invoice** = the existing monthly job, **changed to bill overages only** (drop the
  `BaseFee` line from `GenerateInvoiceForPeriodAsync`). Stays idempotent per (tenant, month) but now
  scoped to `Purpose.Usage`, so it never collides with subscription invoices.

Net: base fee is term-driven (lifecycle); overages are metered monthly. Both legitimate, no overlap.

## Lifecycle flows

### Create tenant

`CreateTenantCommand` gains `string? PlanKey` (optional). Handler (`Multitenancy`):

1. Resolve plan key: use `PlanKey`; if null/empty fall back to a configured default/trial plan
   (`BillingOptions.DefaultPlanKey`, aligned with `QuotaOptions.DefaultPlan`, e.g. `"free"`).
2. Dispatch `GetPlanTermQuery(planKey)` → `PlanTermDto { PlanId, Interval, TermMonths, UnitPrice,
   Currency }`. Not found / inactive ⇒ validation error (400).
3. Compute period: `periodStart = now`, `periodEnd = now.AddMonths(TermMonths)`.
4. `tenantService.CreateAsync(... , planKey, validUpto: periodEnd)` — sets `tenant.Plan = planKey` and
   `ValidUpto = periodEnd` (replaces the hardcoded +1 month). Refresh the Finbuckle distributed cache.
5. Buffer admin password; start provisioning (unchanged).
6. Publish `TenantSubscribedIntegrationEvent { TenantId, PlanId, PlanKey, PeriodStartUtc, PeriodEndUtc,
   Amount, Currency }` via the outbox.

Billing inbox handler `TenantSubscribedIntegrationEventHandler`:
- Cancel any existing active `Subscription`; create a new one (`StartUtc = periodStart`,
  `EndUtc = periodEnd`).
- Create a `Subscription`-purpose `Invoice` (Draft) with a `BaseFee` line (qty 1, unit = term price)
  and `Issue()` it (so it's a real bill with a due date). Skip the invoice if term price == 0 (trial).
- Publish `InvoiceIssuedIntegrationEvent` (for Notifications). Idempotent via Inbox.

### Renew / change plan

Replace `UpgradeTenant` (explicit date) with `RenewTenantCommand { TenantId, PlanKey? }` (Multitenancy):

1. Load tenant + current plan. Target plan = `PlanKey ?? tenant.Plan`. `GetPlanTermQuery(target)`.
2. `periodStart = max(now, tenant.ValidUpto)` (stack remaining time; don't backdate). `periodEnd =
   periodStart.AddMonths(TermMonths)`.
3. `tenant.SetValidity(periodEnd)`; if plan changed, `tenant.Plan = target` (re-resolves quotas).
   Persist + **refresh cache** (fixes the latent `UpgradeSubscriptionAsync` cache-staleness bug).
4. Publish `TenantRenewedIntegrationEvent { …, PlanChanged }`. Billing handler swaps subscription if
   the plan changed and issues a new `Subscription` invoice for the term.

Keep a minimal operator override `AdjustTenantValidityCommand { TenantId, ValidUpto }` (no invoice,
audit-noted) for comps/support — replaces the raw explicit-date path without the billing side-effect.

### Expiry + grace enforcement

- Add `BillingOptions.GraceWindowDays` (config, default 7).
- **Middleware guard** (`MultitenancyModule` post-auth, currently `IsActive`-only): also reject
  non-root tenants where `now > ValidUpto + grace` with `ForbiddenException` ("subscription expired").
  Between `ValidUpto` and `ValidUpto + grace`: allow, optionally add a response header
  `X-Subscription-Grace: <daysLeft>`.
- **Login/refresh** (`IdentityService.ValidateTenantStatus`): align the expiry check to
  `ValidUpto + grace` (a tenant in grace can still log in). Keep root exempt.
- `GetTenantStatusDto` gains `Plan`, and a derived `ExpiryState` (`Active` | `InGrace` | `Expired`) +
  `GraceEndsUtc`, so the admin UI can show the right badge.

## Notifications (Notifications module)

A Hangfire **recurring** `TenantExpiryScanJob` scans tenants and publishes (idempotent per
tenant+state+day):
- `TenantNearingExpiryIntegrationEvent` (within N days of `ValidUpto`, configurable).
- `TenantEnteredGraceIntegrationEvent` (past `ValidUpto`, within grace).
- `TenantExpiredIntegrationEvent` (past grace).

Notifications handlers (`IntegrationEventHandlers/`, `sealed`) consume these + `InvoiceIssued` and send
email via the existing notification/email pipeline + templates. Renewal stays manual — the scan job
only notifies, never renews.

## PDF / downloadable invoices

- Add an on-demand renderer (QuestPDF — MIT-licensed engine; note QuestPDF Community license terms) in
  Billing: `IInvoicePdfRenderer.Render(invoice) → byte[]`.
- New endpoint `GET /billing/invoices/{id}/pdf` (operator + tenant-self via existing permission split,
  mirroring `GetInvoiceById` / `GetMyInvoices`) returns `application/pdf`.
- On-demand generation (no stored artifact) for v1; persisting to the Files module is a later option.

## Admin UI (clients/admin) — built with the frontend-design skill

(Exact component paths pending the admin-UI exploration; will reuse existing shadcn-style primitives —
Dialog, Table, Form (react-hook-form + zod), Select, Badge, Button — not invent new ones.)

- **Create-tenant dialog:** add a **Plan** `<Select>` (options from `GetPlans`, showing name + interval
  + price). Mandatory with the trial default preselected.
- **Tenants list:** add a **Plan** column and an **expiry/status** badge (Active / In grace / Expired,
  driven by `ExpiryState`), alongside the existing active/validity display.
- **Tenant detail / actions:** show plan, `ValidUpto`, grace state, current subscription + recent
  invoices; **Renew** and **Change plan** actions (call `RenewTenant`); invoice list with **Download
  PDF**.
- **Plans admin page:** CRUD over plans incl. the new interval + annual-price fields (extends existing
  `CreatePlan` / `UpdatePlan` / `GetPlans`).
- **Invoices view:** list/filter invoices, mark-paid / issue / void (existing endpoints), PDF download.
- Routing: register lazy routes + mirror permissions in RouteGuard + add nav entries (Billing/Plans),
  following the existing pattern. Playwright route-mocked tests for the new pages.

## Module wiring checklist

- Billing: register new integration event handlers (`AddIntegrationEventHandlers` already scans the
  assembly); register `TenantExpiryScanJob` recurring job; register `IInvoicePdfRenderer`.
- Add the two new `.Contracts` project references (edges above).
- Migrations: one Billing migration (plan columns + invoice columns) in
  `FSH.Starter.Migrations.PostgreSQL` under the Billing folder. No tenant-store schema change.
- Seed: ensure default/trial plan exists with an `Interval` (demo seed + `seed`/`seed-demo`).
- No new module ⇒ no change to the four Mediator/`moduleAssemblies` wire points.

## Error handling

- Plan not found / inactive at create/renew ⇒ FluentValidation failure → 400.
- Billing side-effect failure (subscription/invoice) ⇒ outbox retry (≤ `OutboxMaxRetries`), then
  dead-letter; tenant creation/renew already succeeded (validity set). Surfaced via existing outbox
  dead-letter handling; admin can regenerate the invoice.
- Idempotency: inbox dedup on event id + handler name; subscription-invoice creation also guards on an
  existing `Subscription`-purpose invoice for the same period (no duplicate on redelivery).
- Background handlers carry no tenant context — if any handler touches a tenant-filtered context,
  restore Finbuckle context first (`eventing.md` gotcha). Billing's `BillingDbContext` is not
  tenant-filtered (explicit `TenantId` column), so this is mostly N/A here.

## Testing

- **Unit:** `BillingPlan.GetTermPrice/GetTermMonths`; `Invoice` purpose/period + state machine;
  command validators (`CreateTenant` with/without plan, `RenewTenant`).
- **Integration (Testcontainers):**
  - Create tenant with a plan ⇒ subscription + issued `Subscription` invoice exist; `ValidUpto` =
    period end; `tenant.Plan` set.
  - Trial/zero-price plan ⇒ no invoice, validity still set.
  - Renew same plan ⇒ validity extends by term, new invoice; change plan ⇒ subscription swapped.
  - Expiry: request just past `ValidUpto` within grace ⇒ allowed; past grace ⇒ 403; login in grace ⇒
    allowed, past grace ⇒ 401/expired. Cross-tenant isolation on invoices.
  - Monthly job ⇒ overage-only invoice (no base-fee line), no collision with subscription invoices.
- **Frontend (Playwright, route-mocked):** create-tenant with plan; tenants list badges; renew/change
  plan; plans CRUD; invoice PDF download.

## Phasing (implementation order)

1. **Phase 1 — core backend:** plan interval/price model + migration; invoice purpose/period +
   migration + monthly-job overage-only change; `GetPlanTermQuery`; `TenantSubscribed`/`TenantRenewed`
   events + Billing handlers; `CreateTenant` plan wiring; `RenewTenant` (replace `UpgradeTenant`);
   expiry + grace enforcement (middleware + login + status DTO); integration tests.
2. **Phase 2 — admin UI:** create-tenant plan selector; tenants list plan/expiry badges; tenant detail
   renew/change-plan + invoices; plans CRUD interval fields; invoices view; routes/permissions/nav;
   Playwright tests. (frontend-design skill.)
3. **Phase 3 — notifications:** scan job + expiry events + Notifications handlers/templates.
4. **Phase 4 — PDF invoices:** renderer + download endpoint + UI button.

## Docs (golden rule #10)

Update the separate docs repo (`github.com/fullstackhero/docs`) + add a changelog entry once the
user-facing behavior lands. New/changed config keys: `BillingOptions.DefaultPlanKey`,
`BillingOptions.GraceWindowDays`, expiry-notification lead time.

## Out of scope (deferred)

Payment gateway (Stripe), self-service signup/checkout, auto-renew recurring billing, mid-term
proration, multi-currency conversion, dunning beyond the single grace window.

# Tenant Billing — Production Hardening & Completion Implementation Plan

> **For agentic workers:** execute task-by-task with TDD (red→green→commit). Steps use checkbox syntax.
> Spec: `superpowers/specs/2026-05-28-tenant-billing-production-hardening-design.md`. Read it first.

**Goal:** Complete the tenant-billing SaaS feature to production grade — backend hardening, expiry/renewal
notifications, PDF invoices, tenant-facing dashboard (view + warnings), and full regression/integration tests.

**Architecture:** Invoice-as-record (no gateway), no proration, event-driven side effects via in-memory
`IEventBus`, daily Hangfire scan for expiry notifications, on-demand QuestPDF behind `IInvoicePdfRenderer`.

**Tech Stack:** .NET 10, EF Core 10, Mediator, FluentValidation, Hangfire, QuestPDF, xUnit + Shouldly +
NSubstitute + Testcontainers, React 19 + Vite + TanStack Query + Playwright.

**Conventions (every task):** handlers `public sealed`, `ValueTask<T>`, `.ConfigureAwait(false)` on awaits,
propagate `CancellationToken`, structured logging, `TreatWarningsAsErrors`. Build clean + green tests
before each commit. Verify current code before writing a test (the audit had wrong claims — e.g. indexes
already exist).

---

## Phase A — Backend hardening

### Task A1 — Fix the renewal clock bug
**Files:** Modify `src/Modules/Multitenancy/Modules.Multitenancy/Services/TenantService.cs:203`.
- [ ] Add/confirm `Multitenancy.Tests` (or integration) test that renewal stacking uses the injected clock:
      with a `FakeTimeProvider` set to T, renew a tenant whose `ValidUpto < T` ⇒ new `ValidUpto == T + term`.
- [ ] Replace `var now = DateTime.UtcNow;` with `var now = _timeProvider.GetUtcNow().UtcDateTime;`.
- [ ] Build + test green. Commit `fix(billing): use injected TimeProvider in TenantService.RenewAsync`.

### Task A2 — `X-Subscription-Grace` response header
**Files:** Modify `MultitenancyModule.cs` grace branch (~lines 194-202).
- [ ] Integration test: a non-root tenant with `ValidUpto < now <= ValidUpto+grace` gets a 2xx with header
      `X-Subscription-Grace` = ceil(days left); an Active tenant has no header; past grace ⇒ 403 (unchanged).
- [ ] In the grace branch, before `await next`, when `nowUtc <= ValidUpto+grace && nowUtc > ValidUpto`, set
      `ctx.Response.Headers["X-Subscription-Grace"] = ((int)Math.Ceiling((tenant.ValidUpto.AddDays(graceDays) - nowUtc).TotalDays)).ToString(CultureInfo.InvariantCulture)`.
      Use `ctx.Response.OnStarting` if headers risk being sent — simplest: set before calling next (guard endpoints don't write before).
- [ ] Build + test green. Commit `feat(billing): emit X-Subscription-Grace header during grace window`.

### Task A4 — `AdjustTenantValidityCommand` (operator override)
**Files:**
- Create `…Contracts/v1/AdjustTenantValidity/AdjustTenantValidityCommand.cs` + `…Response.cs`.
- Create `…Multitenancy/Features/v1/AdjustTenantValidity/{Handler,Validator,Endpoint}.cs`.
- Add `ITenantService.AdjustValidityAsync` + impl in `TenantService.cs`.
- Register endpoint in `MultitenancyModule.MapEndpoints`.
- Test: `Integration.Tests/Tests/Multitenancy/AdjustTenantValidityTests.cs`.

Contract: `record AdjustTenantValidityCommand(string TenantId, DateTime ValidUpto) : ICommand<AdjustTenantValidityCommandResponse>;`
Response: `record AdjustTenantValidityCommandResponse(string TenantId, DateTime ValidUpto);`
Service: `Task<DateTime> AdjustValidityAsync(string id, DateTime validUpto, CancellationToken ct)` — load tenant,
`tenant.ValidUpto = DateTime.SpecifyKind(validUpto, DateTimeKind.Utc)` (direct set ⇒ allows backdating for
comps/immediate-expire), `UpdateAsync` + `RefreshTenantCacheAsync`, log `[Multitenancy] adjusted validity…`. No event, no invoice.
Endpoint: `POST /{id}/adjust-validity`, `RequirePermission(MultitenancyPermissions.Tenants.UpgradeSubscription)`
(reuse — root-only operator validity action), body/route id match guard like RenewTenant.
Validator: `TenantId` NotEmpty; `ValidUpto` must be a real date (not default).
- [ ] Tests (red): sets ValidUpto (future + past), no Subscription/Invoice created, root-only (403 for tenant admin), reflected in GetStatus.
- [ ] Implement contract → service → handler → validator → endpoint → register.
- [ ] Build + tests green. Commit `feat(billing): operator AdjustTenantValidity override (no invoice)`.

### Task A-E — Backend regression tests (verify-then-fill; many may already exist)
**Files:** `Integration.Tests/Tests/Multitenancy/*`, `Integration.Tests/Tests/Billing/*`, `Billing.Tests/*`.
For each, first read the existing test file to confirm it's missing, then add only the gap:
- [ ] Expiry-state boundaries in `TenantService.GetStatusAsync` (unit, `FakeTimeProvider`): now==ValidUpto⇒Active,
      now==graceEnds⇒InGrace, now==graceEnds+1s⇒Expired, now==ValidUpto+1s⇒InGrace.
- [ ] Grace enforcement boundaries (middleware + Identity login): at ValidUpto allowed, at graceEnds allowed,
      graceEnds+1s ⇒ 403 (middleware) / 401 (login).
- [ ] Renewal: double-renew stacks ≈2 terms; renew-in-grace starts from now; renew-with-different-plan swaps
      active subscription (old Cancelled, EndUtc==new.StartUtc) + new subscription invoice + PlanChanged=true.
- [ ] Single-active-subscription invariant after plan change; forced second active row violates
      `ux_subscriptions_tenantid_active`.
- [ ] Event-handler idempotency: redeliver same `TenantSubscribedIntegrationEvent` Id ⇒ one subscription + one invoice.
- [ ] Plan seeding: `BillingDbInitializer` seeds free/pro/pro-annual w/ correct interval/price; idempotent on re-run.
- [ ] Invoice state machine at endpoint: Void→mark-paid rejected; Paid→void rejected.
- [ ] Commit per coherent group `test(billing): …`.

**Phase A gate:** `dotnet build src/FSH.Starter.slnx` clean; `dotnet test` Billing+Multitenancy+Integration green.

---

## Phase B — Notifications (read patterns just-in-time, then implement)

### Task B1 — Integration events
- [ ] Create in `Multitenancy.Contracts/Events/`: `TenantNearingExpiryIntegrationEvent` (+`int DaysRemaining`),
      `TenantEnteredGraceIntegrationEvent`, `TenantExpiredIntegrationEvent`. Fields mirror
      `TenantSubscribedIntegrationEvent` + `TenantName, AdminEmail, PlanKey, ValidUpto, GraceEndsUtc`.
- [ ] Create in `Billing.Contracts/Events/`: `InvoiceIssuedIntegrationEvent(Id, OccurredOnUtc, TenantId,
      CorrelationId, Source, InvoiceId, InvoiceNumber, decimal Amount, string Currency, DateTime? DueAtUtc, int PeriodYear, int PeriodMonth)`.
- [ ] Commit `feat(billing): add expiry + invoice-issued integration events`.

### Task B2 — Publish `InvoiceIssued` from the subscription-invoice handlers
- [ ] In `BillingService.CreateSubscriptionInvoiceAsync` (or the two integration-event handlers after issue),
      publish `InvoiceIssuedIntegrationEvent` via the same `IEventBus` already injected. Only when an invoice
      was actually issued (skip free/zero-price). Integration test asserts publish on subscription invoice.
- [ ] Commit.

### Task B3 — `TenantExpiryNotice` dedup entity in `TenantDbContext`
- [ ] Domain entity `Modules.Multitenancy/Domain/TenantExpiryNotice.cs` (`Id, TenantId, NoticeType (string/enum),
      ValidUptoUtc, CreatedAtUtc`), EF config with unique `(TenantId, NoticeType, ValidUptoUtc)`, `DbSet` on TenantDbContext.
- [ ] Migration in `FSH.Starter.Migrations.PostgreSQL` (tenant schema). Full-build before `migrations add`.
- [ ] Commit.

### Task B4 — `TenantExpiryScanJob` (Multitenancy)
- [ ] `Services/TenantExpiryScanJob.cs` `RunAsync(CancellationToken)`; inject `IMultiTenantStore<AppTenantInfo>`,
      `IEventBus`, `IOptions<TenantBillingOptions>`, `TenantDbContext`, `TimeProvider`, `ILogger`. For each active
      non-root tenant compute state; check-or-insert `TenantExpiryNotice`; publish the one matching event; per-tenant try/catch.
- [ ] Register recurring in `MultitenancyModule.MapEndpoints`: `IRecurringJobManager.AddOrUpdate("tenant-expiry-scan",
      Job.FromExpression<TenantExpiryScanJob>(j => j.RunAsync(CancellationToken.None)), "0 2 * * *", new RecurringJobOptions{TimeZone=TimeZoneInfo.Utc})`.
- [ ] Add `TenantBillingOptions.ExpiryNotificationLeadDays = 7`.
- [ ] Unit tests (NSubstitute `IEventBus`): correct event per state incl. boundaries; root/inactive excluded.
      Integration: run twice ⇒ dedup prevents second publish.
- [ ] Commit `feat(billing): daily tenant expiry scan job + dedup ledger`.

### Task B5 — Notifications email handlers
- [ ] `Modules.Notifications/IntegrationEventHandlers/{Nearing,EnteredGrace,Expired,InvoiceIssued}EmailHandler.cs`
      (sealed, `IIntegrationEventHandler<T>`), inject `IMailService` + `ILogger`. Build `MailRequest` via private
      `BillingEmailBodies` helper (subject + HTML). try/catch + warn-log (never throw).
- [ ] Ensure `Modules.Notifications` references `Multitenancy.Contracts` + `Billing.Contracts`.
- [ ] Integration test: subscription invoice issue ⇒ `IMailService.SendAsync` received (fake/substitute mail service in the factory).
- [ ] Commit `feat(notifications): email tenant on expiry states + invoice issued`.

**Phase B gate:** build clean; tests green; `dotnet run --project DbMigrator -- apply` works locally.

---

## Phase C — PDF invoices

### Task C1 — `IInvoicePdfRenderer` + QuestPDF impl
- [ ] Add QuestPDF package to `Modules.Billing`; set `QuestPDF.Settings.License = LicenseType.Community` at module init.
- [ ] `Services/IInvoicePdfRenderer.cs` `byte[] Render(InvoiceDto invoice)`; `Services/InvoicePdfRenderer.cs`
      builds A4: header (number/status/dates), bill-to tenant, period, line-items table, subtotal, notes; 2dp currency.
- [ ] Register in DI. Unit test: returns non-empty `%PDF`-prefixed bytes for a representative `InvoiceDto`.
- [ ] Commit `feat(billing): on-demand invoice PDF renderer (QuestPDF)`.

### Task C2 — PDF endpoints
- [ ] `GET /api/v1/billing/invoices/{id}/pdf` (operator, Billing.View) and `GET …/invoices/me/{id}/pdf`
      (tenant-self, 404 cross-tenant). Return `Results.File(bytes, "application/pdf", $"{number}.pdf")`.
- [ ] Integration tests: operator 200 any invoice; tenant-self 200 own, 404 other tenant.
- [ ] Commit `feat(billing): invoice PDF download endpoints`.

**Phase C gate:** build clean; tests green.

---

## Phase D — Dashboard self-serve + admin glue (read existing FE just-in-time)

### Task D1 — `tenants/me/status` endpoint (backend)
- [ ] `GET /api/v1/tenants/me/status` resolving the calling tenant from context; returns trimmed status
      (plan, validUpto, expiryState, graceEndsUtc). Any authenticated tenant user. Integration test + 401 unauth.
- [ ] Commit.

### Task D2 — Dashboard subscription page + expiry banner + invoice detail + PDF (clients/dashboard)
- [ ] api/billing.ts: fix `SubscriptionStatus` to `Active|Suspended|Cancelled`; type invoice line items; add
      `getMyStatus()` + `getMyInvoice(id)` + `invoicePdfUrl(id)`.
- [ ] `/subscription` page (plan, validity, expiry badge, usage with limits/overage, recent invoices), nav entry, lazy route.
- [ ] Global expiry/grace banner in AppShell driven by `getMyStatus` (staleTime ~5m); shows for InGrace + within lead days.
- [ ] Invoice detail page with line items + Download PDF.
- [ ] Playwright: subscription page renders; banner shows for InGrace + nearing mocks; invoice detail PDF download request.
- [ ] Commit `feat(dashboard): tenant subscription page + expiry banners + invoice PDF`.

### Task D3 — Admin glue (clients/admin)
- [ ] Download PDF button on invoice detail (`invoices/{id}/pdf`).
- [ ] Adjust-validity dialog on tenant detail (date picker → `POST /tenants/{id}/adjust-validity`), root-gated.
- [ ] Plan-form client validation: non-negative monthlyBasePrice/annualPrice/overage.
- [ ] Playwright: PDF button request; adjust-validity posts; plan-form rejects negatives.
- [ ] Commit `feat(admin): invoice PDF download + adjust-validity + plan-form validation`.

**Phase D gate:** `npm run build` + `npx playwright test` green in both apps (route-mocked).

---

## Final
- [ ] `dotnet build src/FSH.Starter.slnx` (TreatWarningsAsErrors) + full `dotnet test` (Docker up).
- [ ] Both frontends: typecheck/lint/build + Playwright.
- [ ] Docs repo (`C:\Users\mukesh\repos\fullstackhero\docs`) + changelog (golden rule #10): new config key,
      new endpoints, QuestPDF license note, dashboard subscription page + banners, expiry/renewal emails.
- [ ] Update memory `project_tenant_billing_lifecycle.md` (Phases 3–4 + dashboard + tests done).

## Self-review notes
- Spec coverage: A1/A2/A4 + A-E (A3 dropped — indexes already exist), B1–B5, C1–C2, D1–D3, Final → all spec
  sections mapped. No payment-gateway / proration tasks (out of scope, confirmed).
- Verify-before-write is mandatory: the audit wrongly claimed missing indexes; existing tests
  (RenewTenantTests, TenantExpiryEnforcementTests, BillingEndpointTests, etc.) cover happy paths — only add gaps.

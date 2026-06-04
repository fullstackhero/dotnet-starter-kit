# Tenant Billing Lifecycle — Phase 1 (Core Backend) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans (inline) or
> subagent-driven-development. Steps use checkbox (`- [ ]`) syntax. Spec:
> `superpowers/specs/2026-05-28-tenant-billing-lifecycle-design.md`.

**Goal:** Wire the Multitenancy tenant lifecycle to the Billing module so creating/renewing a tenant
drives a plan-based subscription + invoice and sets `ValidUpto` from the plan's billing interval, and
so expired tenants are blocked (with a grace window) on every request — not just at login.

**Architecture:** Event-driven (Approach 1). Multitenancy stays the authority over the Finbuckle
tenant store. On create/renew it (a) reads the plan term via a synchronous Mediator query in
`Billing.Contracts`, (b) sets `ValidUpto`/`Plan`, (c) publishes an integration event via
`IEventBus.PublishAsync` (Files precedent). Billing's `IIntegrationEventHandler` creates the
subscription + issues the invoice, idempotently. Two new Contracts edges only — no runtime→runtime.

**Tech Stack:** .NET 10, EF Core 10/PostgreSQL, Mediator 3.x (source-gen), FluentValidation,
Finbuckle, in-memory `IEventBus`, xUnit + Shouldly + Testcontainers.

**Conventions reminder:** handlers `public sealed`, `ValueTask<T>`, `.ConfigureAwait(false)` on every
await, `CancellationToken` propagated; every command + paginated query needs a `{Name}Validator`;
structured logging only; `TreatWarningsAsErrors` (warnings fail the build).

---

## File map

**Billing.Contracts (`src/Modules/Billing/Modules.Billing.Contracts/`)**
- Create `v1/Plans/PlanInterval.cs` — `enum PlanInterval { Monthly = 0, Yearly = 1 }`.
- Create `v1/Invoices/InvoicePurpose.cs` — `enum InvoicePurpose { Subscription = 0, Usage = 1 }`.
- Create `v1/Plans/GetPlanTerm/GetPlanTermQuery.cs` + `PlanTermResponse.cs`.
- Modify plan DTO(s) (`v1/Plans/*`) + `CreatePlan`/`UpdatePlan` command records to carry
  `Interval` + `AnnualPrice`.
- Create `Events/TenantSubscribedIntegrationEvent.cs`, `Events/TenantRenewedIntegrationEvent.cs`
  *(see note: defined in Multitenancy.Contracts, not Billing — corrected below)*.

**Multitenancy.Contracts (`src/Modules/Multitenancy/Modules.Multitenancy.Contracts/`)**
- Create `Events/TenantSubscribedIntegrationEvent.cs`, `Events/TenantRenewedIntegrationEvent.cs`
  (Multitenancy owns + publishes them; Billing references Multitenancy.Contracts to handle).
- Modify `v1/CreateTenant/CreateTenantCommand.cs` — add `string? PlanKey`.
- Create `v1/RenewTenant/RenewTenantCommand.cs` + `RenewTenantCommandResponse.cs`.
- Modify `Dtos/TenantStatusDto.cs` — add `Plan`, `ExpiryState`, `GraceEndsUtc`.

**Billing runtime (`src/Modules/Billing/Modules.Billing/`)**
- Modify `Domain/BillingPlan.cs` — add `Interval`, `AnnualPrice`, `GetTermPrice()`, `GetTermMonths()`.
- Modify `Domain/Invoice.cs` — add `Purpose`, `PeriodStartUtc`, `PeriodEndUtc`; `CreateDraft` overload.
- Modify `Data/Configurations/BillingPlanConfiguration.cs`, `InvoiceConfiguration.cs`.
- Create `Features/v1/Plans/GetPlanTerm/GetPlanTermQueryHandler.cs`.
- Create `IntegrationEventHandlers/TenantSubscribedIntegrationEventHandler.cs` (+ Renewed).
- Modify `Services/BillingService.cs` — `GenerateInvoiceForPeriodAsync` drops the base-fee line
  (overage-only); add a `CreateSubscriptionInvoiceAsync` used by the event handler.
- Modify `Features/v1/Plans/CreatePlan/*`, `UpdatePlan/*` (+ validators) for interval/annual price.
- Modify `BillingModule.cs` — `AddIntegrationEventHandlers(typeof(BillingModule).Assembly)`.
- Add `Modules.Billing.csproj` ref → `Modules.Multitenancy.Contracts`.

**Multitenancy runtime (`src/Modules/Multitenancy/Modules.Multitenancy/`)**
- Modify `Services/TenantService.cs` — `CreateAsync` accepts `planKey` + `validUpto`; add
  `RenewAsync`; `GetStatusAsync` fills new DTO fields; reuse `RefreshTenantCacheAsync` on renew.
- Modify `Features/v1/CreateTenant/CreateTenantCommandHandler.cs` + `Validator` + `Endpoint`.
- Create `Features/v1/RenewTenant/` (handler, validator, endpoint). Remove `UpgradeTenant/`.
- Modify `Services/ITenantService.cs` (Contracts) signatures.
- Modify `MultitenancyModule.cs` — middleware ValidUpto+grace check; map RenewTenant; remove
  UpgradeTenant mapping.
- Add `Modules.Multitenancy.csproj` ref → `Modules.Billing.Contracts`.

**Identity runtime**
- Modify `Services/IdentityService.cs` `ValidateTenantStatus` — expiry uses `ValidUpto + grace`.

**Config / options**
- Create `BillingOptions` (Billing) with `DefaultPlanKey` (default `"free"`) + `GraceWindowDays`
  (default 7). Bound in `BillingModule`. Grace also read by Multitenancy middleware + Identity →
  put `GraceWindowDays` on a shared options type readable by all three (see Task 9).

**Migrations**
- `src/Host/FSH.Starter.Migrations.PostgreSQL/<Billing folder>` — one migration: plan columns +
  invoice columns (backfill `Purpose = Usage`).

**Tests (`src/Tests/`)**
- `Billing.Tests` unit: `BillingPlanTests`, `InvoicePurposeTests`.
- `Integration.Tests/Tests/Billing/TenantBillingLifecycleTests.cs`.
- `Integration.Tests/Tests/Multitenancy/TenantExpiryEnforcementTests.cs`.

> **Correction (single source of truth):** `TenantSubscribed`/`TenantRenewed` events live in
> **Multitenancy.Contracts** (publisher owns the contract). Ignore the Billing.Contracts bullet above.

---

## Task 1: Plan interval + price on `BillingPlan`

**Files:**
- Create: `src/Modules/Billing/Modules.Billing.Contracts/v1/Plans/PlanInterval.cs`
- Modify: `src/Modules/Billing/Modules.Billing/Domain/BillingPlan.cs`
- Test: `src/Tests/Billing.Tests/Domain/BillingPlanTests.cs`

- [ ] **Step 1: Add the enum (Contracts)**
```csharp
namespace FSH.Modules.Billing.Contracts.v1.Plans;

public enum PlanInterval
{
    Monthly = 0,
    Yearly = 1,
}
```

- [ ] **Step 2: Failing unit test for term price/length**
```csharp
using FSH.Modules.Billing.Contracts.v1.Plans;
using FSH.Modules.Billing.Domain;
using Shouldly;
using Xunit;

namespace FSH.Modules.Billing.Tests.Domain;

public class BillingPlanTests
{
    [Fact]
    public void Monthly_plan_term_is_one_month_at_monthly_price()
    {
        var plan = BillingPlan.Create("pro", "Pro", "USD", 30m, PlanInterval.Monthly, annualPrice: null);
        plan.GetTermMonths().ShouldBe(1);
        plan.GetTermPrice().ShouldBe(30m);
    }

    [Fact]
    public void Yearly_plan_uses_annual_price_when_set()
    {
        var plan = BillingPlan.Create("pro-yr", "Pro Annual", "USD", 30m, PlanInterval.Yearly, annualPrice: 300m);
        plan.GetTermMonths().ShouldBe(12);
        plan.GetTermPrice().ShouldBe(300m);
    }

    [Fact]
    public void Yearly_plan_falls_back_to_twelve_times_monthly()
    {
        var plan = BillingPlan.Create("pro-yr", "Pro Annual", "USD", 30m, PlanInterval.Yearly, annualPrice: null);
        plan.GetTermPrice().ShouldBe(360m);
    }
}
```

- [ ] **Step 3: Run → FAIL** (`Create` overload + members don't exist).
Run: `dotnet test src/Tests/Billing.Tests --filter FullyQualifiedName~BillingPlanTests`

- [ ] **Step 4: Implement on `BillingPlan`** — add fields + members; extend `Create`/`Update`.
```csharp
// add to using:
using FSH.Modules.Billing.Contracts.v1.Plans;

// add properties (after MonthlyBasePrice):
public PlanInterval Interval { get; private set; } = PlanInterval.Monthly;
public decimal? AnnualPrice { get; private set; }

// extend Create signature with: PlanInterval interval, decimal? annualPrice
// set: Interval = interval; AnnualPrice = annualPrice >= 0 ? annualPrice : throw ...
// (validate annualPrice is null or >= 0)

// extend Update signature with: PlanInterval interval, decimal? annualPrice (set both)

public int GetTermMonths() => Interval == PlanInterval.Yearly ? 12 : 1;

public decimal GetTermPrice() =>
    Interval == PlanInterval.Yearly
        ? (AnnualPrice ?? (MonthlyBasePrice * 12m))
        : MonthlyBasePrice;
```
Update the existing `Create`/`Update` callers (CreatePlan/UpdatePlan handlers + seed) to pass the new
args (default `PlanInterval.Monthly, null` where unspecified) — fixed in Task 7.

- [ ] **Step 5: Run → PASS.**

- [ ] **Step 6: Commit** `feat(billing): add billing interval + term price to BillingPlan`

---

## Task 2: `BillingPlan` EF config + DTO + Create/Update plumbing

**Files:**
- Modify: `src/Modules/Billing/Modules.Billing/Data/Configurations/BillingPlanConfiguration.cs`
- Modify: `Contracts/v1/Plans/*` (PlanDto, CreatePlanCommand, UpdatePlanCommand)
- Modify: `Features/v1/Plans/CreatePlan/CreatePlanCommandHandler.cs` + `Validator`,
  `UpdatePlan/UpdatePlanCommandHandler.cs` + `Validator`, `GetPlans` projection.

- [ ] **Step 1:** EF config — map `Interval` (int) + `AnnualPrice` (`numeric(18,2)`, nullable).
- [ ] **Step 2:** Add `Interval` (PlanInterval) + `AnnualPrice` (decimal?) to the plan response DTO and
  `GetPlans` projection.
- [ ] **Step 3:** Add `Interval` + `AnnualPrice` to `CreatePlanCommand` / `UpdatePlanCommand`; pass
  through handlers into `BillingPlan.Create/Update`.
- [ ] **Step 4:** Validators: `Interval` must be defined enum; `AnnualPrice` null or `>= 0`; require
  `AnnualPrice` xor accept null when `Interval == Yearly` (null ⇒ 12× fallback, allowed).
- [ ] **Step 5:** Build. Run: `dotnet build src/FSH.Starter.slnx`. Expected: PASS.
- [ ] **Step 6: Commit** `feat(billing): persist + expose plan interval and annual price`

---

## Task 3: `Invoice` purpose + period span

**Files:**
- Create: `src/Modules/Billing/Modules.Billing.Contracts/v1/Invoices/InvoicePurpose.cs`
- Modify: `src/Modules/Billing/Modules.Billing/Domain/Invoice.cs`
- Modify: `Data/Configurations/InvoiceConfiguration.cs`
- Test: `src/Tests/Billing.Tests/Domain/InvoicePurposeTests.cs`

- [ ] **Step 1:** Enum (Contracts):
```csharp
namespace FSH.Modules.Billing.Contracts.v1.Invoices;
public enum InvoicePurpose { Subscription = 0, Usage = 1 }
```

- [ ] **Step 2: Failing test** — a subscription invoice carries purpose + period span:
```csharp
[Fact]
public void Subscription_draft_carries_purpose_and_period_span()
{
    var start = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
    var end = start.AddMonths(12);
    var inv = Invoice.CreateDraft("acme", "SUB-202605-acme", 2026, 5, "USD",
        InvoicePurpose.Subscription, start, end);
    inv.Purpose.ShouldBe(InvoicePurpose.Subscription);
    inv.PeriodStartUtc.ShouldBe(start);
    inv.PeriodEndUtc.ShouldBe(end);
}
```

- [ ] **Step 3: Run → FAIL.**

- [ ] **Step 4: Implement** — add `Purpose`, `PeriodStartUtc`, `PeriodEndUtc`; add a `CreateDraft`
  overload (keep the existing 5-arg one delegating with `Purpose.Usage, null, null`):
```csharp
public InvoicePurpose Purpose { get; private set; } = InvoicePurpose.Usage;
public DateTime? PeriodStartUtc { get; private set; }
public DateTime? PeriodEndUtc { get; private set; }

public static Invoice CreateDraft(string tenantId, string invoiceNumber, int periodYear,
    int periodMonth, string currency)
    => CreateDraft(tenantId, invoiceNumber, periodYear, periodMonth, currency,
        InvoicePurpose.Usage, null, null);

public static Invoice CreateDraft(string tenantId, string invoiceNumber, int periodYear,
    int periodMonth, string currency, InvoicePurpose purpose,
    DateTime? periodStartUtc, DateTime? periodEndUtc)
{
    // existing guards ...
    var invoice = /* existing init */;
    invoice.Purpose = purpose;
    invoice.PeriodStartUtc = periodStartUtc is { } s ? DateTime.SpecifyKind(s, DateTimeKind.Utc) : null;
    invoice.PeriodEndUtc = periodEndUtc is { } e ? DateTime.SpecifyKind(e, DateTimeKind.Utc) : null;
    return invoice;
}
```

- [ ] **Step 5:** EF config — map `Purpose` (int, default Usage), `PeriodStartUtc`/`PeriodEndUtc`
  (`timestamptz`, nullable). Add `Purpose` to the invoice response DTO + projections.

- [ ] **Step 6: Run → PASS.** Build.
- [ ] **Step 7: Commit** `feat(billing): add invoice purpose + term period span`

---

## Task 4: Monthly job becomes overage-only; add subscription-invoice service

**Files:**
- Modify: `src/Modules/Billing/Modules.Billing/Services/BillingService.cs`
- Modify: `Services/IBillingService.cs`
- Test: covered by integration Task 11 (monthly job no base-fee line).

- [ ] **Step 1:** In `GenerateInvoiceForPeriodAsync`: **remove** the `if (plan.MonthlyBasePrice > 0)`
  base-fee line block (lines ~70-73). The method now only adds overage lines + sets
  `Purpose = Usage`. Change `BuildInvoiceNumber` prefix `INV-` → `USG-`. Pass
  `InvoicePurpose.Usage` to `CreateDraft`. If a draft has zero overage lines, still create it (records
  zero-usage period) — keep current behavior, just without base fee.

- [ ] **Step 2:** Add to `IBillingService`:
```csharp
Task<Invoice?> CreateSubscriptionInvoiceAsync(
    string tenantId, Guid planId, DateTime periodStartUtc, DateTime periodEndUtc,
    CancellationToken cancellationToken = default);
```

- [ ] **Step 3:** Implement `CreateSubscriptionInvoiceAsync` in `BillingService`:
```csharp
public async Task<Invoice?> CreateSubscriptionInvoiceAsync(
    string tenantId, Guid planId, DateTime periodStartUtc, DateTime periodEndUtc,
    CancellationToken cancellationToken = default)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
    var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException($"Plan {planId} not found for tenant {tenantId}.");

    var termPrice = plan.GetTermPrice();
    if (termPrice <= 0)
    {
        return null; // trial / free plan — no invoice
    }

    var periodStart = DateTime.SpecifyKind(periodStartUtc, DateTimeKind.Utc);
    var periodEnd = DateTime.SpecifyKind(periodEndUtc, DateTimeKind.Utc);

    // Idempotency guard: one subscription invoice per (tenant, period start).
    var number = $"SUB-{periodStart:yyyyMM}-{(tenantId.Length <= 8 ? tenantId : tenantId[..8])}";
    var existing = await _db.Invoices
        .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.InvoiceNumber == number, cancellationToken)
        .ConfigureAwait(false);
    if (existing is not null)
    {
        return existing;
    }

    var invoice = Invoice.CreateDraft(tenantId, number, periodStart.Year, periodStart.Month,
        plan.Currency, InvoicePurpose.Subscription, periodStart, periodEnd);
    invoice.AddLineItem(InvoiceLineItemKind.BaseFee,
        $"{plan.Name} — {plan.Interval} subscription ({periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd})",
        1m, termPrice);
    invoice.Issue(); // issue immediately → due +14 days
    _db.Invoices.Add(invoice);
    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return invoice;
}
```

- [ ] **Step 4:** Build. **Commit** `feat(billing): overage-only monthly job + subscription invoice service`

---

## Task 5: `GetPlanTermQuery` (Billing.Contracts) + handler

**Files:**
- Create: `Contracts/v1/Plans/GetPlanTerm/GetPlanTermQuery.cs`, `PlanTermResponse.cs`
- Create: `Features/v1/Plans/GetPlanTerm/GetPlanTermQueryHandler.cs`
- Test: integration Task 10 exercises it end-to-end.

- [ ] **Step 1: Contracts**
```csharp
using Mediator;
namespace FSH.Modules.Billing.Contracts.v1.Plans;

public sealed record GetPlanTermQuery(string PlanKey) : IQuery<PlanTermResponse>;

public sealed record PlanTermResponse(
    Guid PlanId, string Key, string Name, PlanInterval Interval,
    int TermMonths, decimal UnitPrice, string Currency);
```

- [ ] **Step 2: Handler (Billing runtime)** — resolve active plan by lowercased key; throw
  `NotFoundException` if missing/inactive.
```csharp
public sealed class GetPlanTermQueryHandler(BillingDbContext db)
    : IQueryHandler<GetPlanTermQuery, PlanTermResponse>
{
    public async ValueTask<PlanTermResponse> Handle(GetPlanTermQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
#pragma warning disable CA1308
        var key = query.PlanKey.ToLowerInvariant();
#pragma warning restore CA1308
        var plan = await db.Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Key == key && p.IsActive, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Active plan with key '{query.PlanKey}' not found.");
        return new PlanTermResponse(plan.Id, plan.Key, plan.Name, plan.Interval,
            plan.GetTermMonths(), plan.GetTermPrice(), plan.Currency);
    }
}
```

- [ ] **Step 3:** Build. **Commit** `feat(billing): GetPlanTerm contracts query`

---

## Task 6: Integration events + Billing handlers + wiring

**Files:**
- Create: `Multitenancy.Contracts/Events/TenantSubscribedIntegrationEvent.cs`,
  `TenantRenewedIntegrationEvent.cs`
- Create: `Billing/IntegrationEventHandlers/TenantSubscribedIntegrationEventHandler.cs`,
  `TenantRenewedIntegrationEventHandler.cs`
- Modify: `Billing/BillingModule.cs` (register handlers), `Modules.Billing.csproj`
  (ref Multitenancy.Contracts)

- [ ] **Step 1: Events (Multitenancy.Contracts)** — both implement `IIntegrationEvent`:
```csharp
using FSH.Framework.Eventing.Abstractions;
namespace FSH.Modules.Multitenancy.Contracts.Events;

public sealed record TenantSubscribedIntegrationEvent(
    Guid Id, DateTime OccurredOnUtc, string? TenantId, string CorrelationId, string Source,
    Guid PlanId, string PlanKey, DateTime PeriodStartUtc, DateTime PeriodEndUtc)
    : IIntegrationEvent;

public sealed record TenantRenewedIntegrationEvent(
    Guid Id, DateTime OccurredOnUtc, string? TenantId, string CorrelationId, string Source,
    Guid PlanId, string PlanKey, DateTime PeriodStartUtc, DateTime PeriodEndUtc, bool PlanChanged)
    : IIntegrationEvent;
```

- [ ] **Step 2: `Modules.Billing.csproj`** — add
  `<ProjectReference Include="..\..\Multitenancy\Modules.Multitenancy.Contracts\Modules.Multitenancy.Contracts.csproj" />`.

- [ ] **Step 3: Subscribed handler** — create/replace subscription + issue invoice (idempotent):
```csharp
public sealed class TenantSubscribedIntegrationEventHandler(
    BillingDbContext db, IBillingService billing, ILogger<TenantSubscribedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<TenantSubscribedIntegrationEvent>
{
    public async Task HandleAsync(TenantSubscribedIntegrationEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var tenantId = e.TenantId ?? throw new InvalidOperationException("TenantSubscribed missing TenantId");

        var current = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, ct)
            .ConfigureAwait(false);
        current?.Cancel(e.PeriodStartUtc);
        var sub = Subscription.Create(tenantId, e.PlanId, e.PeriodStartUtc);
        // if Subscription supports EndUtc setting on create/renew, set it to PeriodEndUtc
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await billing.CreateSubscriptionInvoiceAsync(tenantId, e.PlanId, e.PeriodStartUtc, e.PeriodEndUtc, ct)
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("[Billing] subscribed tenant {TenantId} to plan {PlanKey} until {End}",
                tenantId, e.PlanKey, e.PeriodEndUtc);
    }
}
```
The Renewed handler is identical except it only swaps the subscription when `e.PlanChanged` is true
(otherwise it keeps the current subscription and just issues the new-term invoice).

- [ ] **Step 4: `BillingModule.ConfigureServices`** — add:
```csharp
builder.Services.AddIntegrationEventHandlers(typeof(BillingModule).Assembly);
```
(`using FSH.Framework.Eventing;`)

- [ ] **Step 5:** Build. **Commit** `feat(billing): handle tenant subscribed/renewed events`

---

## Task 7: Default/seed plan carries an interval

**Files:**
- Modify: Billing seed (the `BillingDbInitializer` / demo seed that creates "free"/"pro" plans).

- [ ] **Step 1:** Update seed `BillingPlan.Create(...)` calls to pass `PlanInterval.Monthly`
  (and an `annualPrice` for any yearly demo plan). Ensure a `"free"` plan exists with
  `MonthlyBasePrice = 0` (trial fallback ⇒ no invoice).
- [ ] **Step 2:** Build. **Commit** `chore(billing): seed plans with billing interval`

---

## Task 8: Migration (plan + invoice columns)

**Files:** `src/Host/FSH.Starter.Migrations.PostgreSQL/<Billing folder>/`

- [ ] **Step 1:** Full build first (avoids the `migrations add` snapshot footgun):
  `dotnet build src/FSH.Starter.slnx`.
- [ ] **Step 2:** Add migration (use the repo's EF migration convention / `create-migration` skill —
  Billing DbContext, output to the Billing folder):
  `dotnet ef migrations add BillingPlanIntervalAndInvoicePurpose -c BillingDbContext -p src/Host/FSH.Starter.Migrations.PostgreSQL -s src/Host/FSH.Starter.Api -o <Billing migrations folder>`
- [ ] **Step 3:** Inspect the generated migration: new columns `Interval int not null default 0`,
  `AnnualPrice numeric(18,2) null` on plans; `Purpose int not null default 1`, `PeriodStartUtc`,
  `PeriodEndUtc timestamptz null` on invoices. Confirm existing rows backfill `Purpose = 1` (Usage).
- [ ] **Step 4:** Apply to a scratch DB: `dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply`.
- [ ] **Step 5: Commit** `chore(db): migration for plan interval + invoice purpose`

---

## Task 9: `BillingOptions` + grace config

**Files:**
- Create: `src/Modules/Billing/Modules.Billing/BillingOptions.cs` (or Contracts if shared).
- Grace needs to be read by Multitenancy middleware + Identity. Put `GraceWindowDays` on a small
  options type in a place all three can reference. Simplest: a `TenantBillingOptions` in
  `BuildingBlocks/Shared/Multitenancy` — **but** that touches BuildingBlocks (needs approval).
  **Chosen instead:** bind the same config section `"Billing"` independently in each module (no shared
  type): a local options record per module reading `Billing:GraceWindowDays` / `Billing:DefaultPlanKey`.

- [ ] **Step 1:** `BillingOptions { string DefaultPlanKey = "free"; int GraceWindowDays = 7; }`; bind
  in `BillingModule` from config section `"Billing"`.
- [ ] **Step 2:** In Multitenancy, add a local `MultitenancyBillingOptions { int GraceWindowDays = 7; }`
  bound from `"Billing"`; inject where the middleware/`TenantService` needs it.
- [ ] **Step 3:** In Identity, read `Billing:GraceWindowDays` (existing options pattern) for the login
  expiry check.
- [ ] **Step 4:** `appsettings` (API + DbMigrator + tests as needed): add
  `"Billing": { "DefaultPlanKey": "free", "GraceWindowDays": 7 }`.
- [ ] **Step 5: Commit** `feat(billing): grace window + default plan options`

---

## Task 10: `CreateTenant` wires plan → validity + event

**Files:**
- Modify: `Multitenancy.Contracts/v1/CreateTenant/CreateTenantCommand.cs` (add `string? PlanKey`)
- Modify: `Multitenancy/Services/ITenantService.cs` + `TenantService.CreateAsync`
- Modify: `Multitenancy/Features/v1/CreateTenant/CreateTenantCommandHandler.cs` + `Validator` +
  `Endpoint`
- Modify: `Modules.Multitenancy.csproj` (ref Billing.Contracts)
- Test: `Integration.Tests/Tests/Billing/TenantBillingLifecycleTests.cs`

- [ ] **Step 1: csproj ref** Billing.Contracts on Multitenancy runtime.

- [ ] **Step 2:** `CreateTenantCommand` gains `string? PlanKey` (last param, optional).

- [ ] **Step 3:** `TenantService.CreateAsync` signature gains `string planKey, DateTime validUpto`;
  set them on the new `AppTenantInfo` before `AddAsync`, then `RefreshTenantCacheAsync(tenant)`:
```csharp
AppTenantInfo tenant = new(id, name, connectionString, adminEmail, issuer)
{
    Plan = planKey,
};
tenant.SetValidity(DateTime.SpecifyKind(validUpto, DateTimeKind.Utc));
await _tenantStore.AddAsync(tenant).ConfigureAwait(false);
await RefreshTenantCacheAsync(tenant).ConfigureAwait(false);
return tenant.Id;
```

- [ ] **Step 4: Handler** — resolve plan, compute period, create, publish event:
```csharp
public sealed class CreateTenantCommandHandler(
    ITenantService tenantService,
    ITenantProvisioningService provisioningService,
    ITenantInitialPasswordBuffer passwordBuffer,
    ISender mediator,
    IEventBus events,
    IOptions<MultitenancyBillingOptions> billingOptions,
    TimeProvider timeProvider)
    : ICommandHandler<CreateTenantCommand, CreateTenantCommandResponse>
{
    public async ValueTask<CreateTenantCommandResponse> Handle(CreateTenantCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var planKey = string.IsNullOrWhiteSpace(command.PlanKey)
            ? billingOptions.Value.DefaultPlanKey : command.PlanKey!;
        var term = await mediator.Send(new GetPlanTermQuery(planKey), ct).ConfigureAwait(false);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var periodEnd = now.AddMonths(term.TermMonths);

        var tenantId = await tenantService.CreateAsync(command.Id, command.Name,
            command.ConnectionString, command.AdminEmail, command.Issuer, term.Key, periodEnd, ct)
            .ConfigureAwait(false);

        passwordBuffer.Store(tenantId, command.AdminPassword);
        var provisioning = await provisioningService.StartAsync(tenantId, ct).ConfigureAwait(false);

        await events.PublishAsync(new TenantSubscribedIntegrationEvent(
            Guid.NewGuid(), now, tenantId, provisioning.CorrelationId, "Multitenancy",
            term.PlanId, term.Key, now, periodEnd), ct).ConfigureAwait(false);

        return new CreateTenantCommandResponse(tenantId, provisioning.CorrelationId,
            provisioning.Status.ToString());
    }
}
```

- [ ] **Step 5: Validator** — `PlanKey`: when provided, lowercase slug `^[a-z0-9][a-z0-9-]{0,62}[a-z0-9]$`
  (else null/empty allowed → default). Keep existing field rules.

- [ ] **Step 6: Endpoint** — add `PlanKey` to the request DTO (optional) + permission unchanged.

- [ ] **Step 7: Integration test** (Testcontainers): create tenant with `PlanKey = "pro"` ⇒
  - tenant `ValidUpto ≈ now + term`, `Plan == "pro"`;
  - exactly one `Subscription` (Active) for the tenant;
  - exactly one `Subscription`-purpose `Invoice`, status `Issued`, subtotal == term price.
  Trial: `PlanKey = "free"` (price 0) ⇒ subscription created, **no** invoice.
  (Set Finbuckle tenant context INLINE per the AsyncLocal gotcha.)

- [ ] **Step 8: Run integration test → PASS.** **Commit**
  `feat(multitenancy): create tenant subscribes to a plan and invoices`

---

## Task 11: `RenewTenant` replaces `UpgradeTenant`

**Files:**
- Create: `Multitenancy.Contracts/v1/RenewTenant/RenewTenantCommand.cs` + response
- Create: `Multitenancy/Features/v1/RenewTenant/{Handler,Validator,Endpoint}.cs`
- Modify: `TenantService` (add `RenewAsync`), `ITenantService`
- Delete: `Multitenancy/Features/v1/UpgradeTenant/*` and `Contracts/v1/UpgradeTenant/*`; remove its
  endpoint mapping; remove `UpgradeSubscriptionAsync` from `ITenantService`/`TenantService`.
- Modify: `MultitenancyModule.MapEndpoints` (map Renew, drop Upgrade).
- Test: `TenantBillingLifecycleTests` renew cases.

- [ ] **Step 1: Command** `RenewTenantCommand(string TenantId, string? PlanKey) : ICommand<RenewTenantCommandResponse>`;
  response `(string TenantId, DateTime ValidUpto, string PlanKey, bool PlanChanged)`.

- [ ] **Step 2: `TenantService.RenewAsync`**:
```csharp
public async Task<(DateTime ValidUpto, string PlanKey, bool PlanChanged)> RenewAsync(
    string id, string newPlanKey, int termMonths, CancellationToken ct = default)
{
    var tenant = await GetTenantInfoAsync(id, ct).ConfigureAwait(false);
    var now = DateTime.UtcNow;
    var basis = tenant.ValidUpto > now ? tenant.ValidUpto : now; // stack remaining time
    var newValidUpto = DateTime.SpecifyKind(basis.AddMonths(termMonths), DateTimeKind.Utc);
    var planChanged = !string.Equals(tenant.Plan, newPlanKey, StringComparison.OrdinalIgnoreCase);
    tenant.SetValidity(newValidUpto);
    if (planChanged) { tenant.Plan = newPlanKey; }
    await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
    await RefreshTenantCacheAsync(tenant).ConfigureAwait(false); // FIX: Upgrade never did this
    return (tenant.ValidUpto, newPlanKey, planChanged);
}
```
Return both the basis-derived `periodStart` (= `basis`) and `newValidUpto` to the handler (extend the
tuple or recompute in handler) so the published event period matches.

- [ ] **Step 3: Handler** — resolve target plan term, renew, publish `TenantRenewedIntegrationEvent`:
```csharp
var planKey = string.IsNullOrWhiteSpace(command.PlanKey) ? /* tenant.Plan */ ... : command.PlanKey!;
var term = await mediator.Send(new GetPlanTermQuery(planKey), ct).ConfigureAwait(false);
// compute basis = max(now, tenant.ValidUpto); periodEnd = basis.AddMonths(term.TermMonths)
var result = await tenantService.RenewAsync(command.TenantId, term.Key, term.TermMonths, ct);
await events.PublishAsync(new TenantRenewedIntegrationEvent(Guid.NewGuid(), now, command.TenantId,
    correlationId, "Multitenancy", term.PlanId, term.Key, periodStart, result.ValidUpto,
    result.PlanChanged), ct);
```
(To get the current plan when `PlanKey` is null, read it from `GetStatusAsync`/tenant — fetch tenant
plan via `tenantService.GetStatusAsync` before resolving term.)

- [ ] **Step 4: Validator** — `TenantId` not empty; `PlanKey` slug when provided.

- [ ] **Step 5: Endpoint** — `POST api/v1/tenants/{id}/renew`, permission
  `MultitenancyPermissions.Tenants.UpgradeSubscription` (reuse existing perm; rename later if desired).

- [ ] **Step 6: Integration tests** — renew same plan ⇒ `ValidUpto` extends by term, new `Subscription`
  invoice issued, subscription unchanged; renew with different plan ⇒ subscription swapped
  (old Cancelled, new Active), `tenant.Plan` updated, invoice for new plan's term.

- [ ] **Step 7: Run → PASS.** **Commit** `feat(multitenancy): plan-driven renew replaces explicit-date upgrade`

---

## Task 12: Expiry + grace enforcement (middleware, login, status DTO)

**Files:**
- Modify: `Multitenancy/MultitenancyModule.cs` (deactivated-tenant guard block, ~lines 164-190)
- Modify: `Identity/Services/IdentityService.cs` `ValidateTenantStatus` (~lines 275-291)
- Modify: `Multitenancy.Contracts/Dtos/TenantStatusDto.cs` + `TenantService.GetStatusAsync`
- Test: `Integration.Tests/Tests/Multitenancy/TenantExpiryEnforcementTests.cs`

- [ ] **Step 1: TenantStatusDto** — add `string? Plan`, `string ExpiryState` (`"Active"`/`"InGrace"`/
  `"Expired"`), `DateTime GraceEndsUtc`. Compute in `GetStatusAsync` from `ValidUpto` + grace days +
  `timeProvider.GetUtcNow()`. Inject `TimeProvider` + `IOptions<MultitenancyBillingOptions>` into
  `TenantService`.

- [ ] **Step 2: Middleware guard** — extend the existing non-operator branch: after the `IsActive`
  check, also reject on expiry past grace:
```csharp
if (tenant is not null &&
    !string.Equals(tenant.Id, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
{
    if (!tenant.IsActive)
        throw new ForbiddenException("This tenant has been deactivated. Contact your administrator.");

    var graceDays = ctx.RequestServices
        .GetRequiredService<IOptions<MultitenancyBillingOptions>>().Value.GraceWindowDays;
    var now = ctx.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
    if (now > tenant.ValidUpto.AddDays(graceDays))
        throw new ForbiddenException("This tenant's subscription has expired. Please renew to continue.");
}
```
(Keep root exempt; keep the claim-fallback tenant resolution already present.)

- [ ] **Step 3: Identity login/refresh** — in `ValidateTenantStatus`, change the expiry check to
  `now > tenant.ValidUpto.AddDays(graceDays)` (inject grace days via existing options). Keeps a
  grace-period tenant able to log in; root exempt unchanged.

- [ ] **Step 4: Integration tests** (`TenantExpiryEnforcementTests`):
  - tenant with `ValidUpto = now - 1d`, grace 7 ⇒ authenticated request **succeeds** (in grace);
  - `ValidUpto = now - 8d`, grace 7 ⇒ request **403**;
  - login while in grace ⇒ **succeeds**; login past grace ⇒ **401/expired**;
  - root operator never blocked.
  Use a real seeded tenant; set its `ValidUpto` via the store; refresh cache.

- [ ] **Step 5: Run → PASS.** **Commit** `feat(multitenancy): enforce expiry with grace window`

---

## Task 13: Phase-1 verification

- [ ] **Step 1:** `dotnet build src/FSH.Starter.slnx` (warnings = errors) → PASS.
- [ ] **Step 2:** `dotnet test src/FSH.Starter.slnx` (Docker up for integration) → PASS.
- [ ] **Step 3:** Architecture tests pass (module boundaries, validators-present). If a new command
  lacks a validator, add it.
- [ ] **Step 4:** Smoke via Aspire/Scalar: create tenant with a plan → verify subscription + issued
  invoice appear in `GET /billing/invoices?tenantId=...`; renew → second invoice; let `ValidUpto`
  lapse past grace → request 403.
- [ ] **Step 5: Commit** any fixups. Phase 1 complete.

---

## Self-review notes (author)

- Spec coverage: plan interval (T1-2,8), invoice purpose/period (T3,8), monthly-job overage-only (T4),
  subscription invoice (T4,6), GetPlanTerm (T5), events+handlers (T6), seed (T7), options/grace (T9),
  create wiring (T10), renew/change-plan replacing upgrade (T11), expiry+grace enforcement (T12),
  verification (T13). Notifications, PDF, and admin UI are **Phases 2-4** (separate plans).
- Type consistency: `PlanInterval` (Contracts), `InvoicePurpose` (Contracts), `GetPlanTermQuery`/
  `PlanTermResponse`, `TenantSubscribed/RenewedIntegrationEvent` (Multitenancy.Contracts),
  `CreateSubscriptionInvoiceAsync`, `RenewAsync` — names used consistently across tasks.
- Deviation logged: publish via `IEventBus.PublishAsync` (Files precedent) rather than the Outbox, to
  avoid adding outbox tables/migrations to TenantDbContext; idempotency enforced manually in the
  Billing handler + the `SUB-{yyyymm}-{tenant8}` invoice-number guard. In-memory bus dispatches
  synchronously, so consistency is effectively immediate; swappable to RabbitMQ later.
- Open verification: confirm `Subscription` exposes a way to set `EndUtc` (Task 6 Step 3). If not, add
  a method on the aggregate in Task 6 (e.g. `Create(tenantId, planId, start, end)` overload).
```

## Phases 2-4 (separate plans, to follow)

- **Phase 2 — Admin UI** (frontend-design skill): create-tenant plan `<Select>`; tenants list plan +
  expiry/grace badge; tenant detail Renew/Change-plan + invoices; plan form interval/annual-price
  fields; invoice purpose display; `api/tenants.ts` + `api/billing.ts` extensions; routes/perms/nav;
  Playwright tests.
- **Phase 3 — Notifications:** `TenantExpiryScanJob` (Hangfire recurring) emits nearing-expiry /
  entered-grace / expired events; Notifications handlers + email templates; `InvoiceIssued` event +
  handler.
- **Phase 4 — PDF invoices:** `IInvoicePdfRenderer` (QuestPDF) + `GET /billing/invoices/{id}/pdf` +
  admin download button.

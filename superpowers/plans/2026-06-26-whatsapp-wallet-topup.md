# WhatsApp Wallet + Top-up Request — Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a tenant (clinic) request a prepaid wallet top-up from the dashboard; let an operator see the request in the admin app, generate an invoice for it, and have the wallet auto-credited when that invoice is marked paid (payments collected manually, offline).

**Architecture:** We are the WhatsApp BSP (Meta bills us; clinics pay us). This phase builds the **funding** half of a prepaid money wallet inside the existing **Billing** module: three new tenant-scoped entities (`Wallet`, `WalletTransaction` ledger, `TopupRequest`), CQRS commands/queries + endpoints, an admin review/approve UI, a dashboard request UI, and one EF migration. Crediting rides the **existing** invoice mark-paid flow — when a `Topup`-purpose invoice transitions to Paid, `BillingService` writes a `+credit` ledger row and completes the request. The **metering** half (debiting the wallet per message) is explicitly out of scope (Phase 2, needs the Meta send integration which does not exist yet).

**Tech Stack:** .NET 10, EF Core 10 (Npgsql), Mediator 3.x (source-gen), FluentValidation 12.x, ASP.NET Minimal APIs + Asp.Versioning, Finbuckle multitenancy, xUnit + Shouldly + Testcontainers (backend); React 19 + Vite 7 + TS, TanStack Query v5, React Router 7 (both frontends).

## Global Constraints

- **Module boundary:** all backend work lives in `src/Modules/Billing/` (runtime `Modules.Billing` + public `Modules.Billing.Contracts`). Do **not** touch `src/BuildingBlocks`.
- **Mediator handlers** are `public sealed`, return `ValueTask<T>`, and `.ConfigureAwait(false)` every await. `ArgumentNullException.ThrowIfNull(...)` guard at the top of every handler.
- **Every command handler + every paginated query handler needs a `{Name}Validator`** (enforced by `Architecture.Tests`).
- **`BillingDbContext` is NOT tenant-filtered** (it is a plain `DbContext`, not `BaseDbContext`). Every billing entity carries an explicit `string TenantId`, and every handler that reads/writes it **must root-gate**: resolve `callerTenantId` from `IMultiTenantContextAccessor<AppTenantInfo>`, treat `callerTenantId == MultitenancyConstants.Root.Id` as root, and scope all other callers to their own tenant. (Mirror `GetInvoicesQueryHandler`.) This is a hard security rule from the prior cross-tenant audit.
- **Enum ordering footgun:** EF writes enums via `HasConversion<int>()`. A property whose value equals the CLR default `0` and that also has `HasDefaultValue` may be omitted on INSERT. Order new enums so the natural initial member is `0` **and** always set it explicitly in the factory.
- **Money:** decimals use `HasPrecision(18, 4)`. Currency strings `HasMaxLength(8)`.
- **TenantId columns:** `IsRequired().HasMaxLength(64)`.
- **Structured logging only** (message templates / `[LoggerMessage]`), never log PII (e.g. emails) into messages.
- **CancellationToken** propagated into every EF/IO call; `= default` on public service methods.
- **Frontend:** admin uses `react-hook-form + zod`; **dashboard uses plain controlled `useState` inputs** (rhf+zod is admin-only). Pass per-call data via `mutate(arg)`, never via state the mutation callback closes over.
- **String enums over the wire:** the API serializes enums as strings globally; frontends mirror them as string unions (e.g. `"Pending" | "Invoiced" | ...`), not numeric consts.
- **Docs travel with the change** (AGENTS rule #10): a changelog entry + docs-repo page update are part of "done" (Task 12).

---

## File Structure

**Backend — new files (`src/Modules/Billing/`)**

Contracts (`Modules.Billing.Contracts/`):
- `WalletEnums.cs` — `WalletStatus`, `TopupRequestStatus` (or fold into existing `BillingEnums.cs`; this plan adds them to `BillingEnums.cs` to match the module's single-enum-file convention).
- `Dtos/WalletDto.cs`, `Dtos/WalletTransactionDto.cs`, `Dtos/TopupRequestDto.cs`
- `v1/Wallets/GetMyWalletQuery.cs`
- `v1/Wallets/CreateTopupRequestCommand.cs`
- `v1/Wallets/GetMyTopupRequestsQuery.cs`
- `v1/Wallets/GetTopupRequestsQuery.cs` (admin, cross-tenant)
- `v1/Wallets/ApproveTopupRequestCommand.cs`
- `v1/Wallets/RejectTopupRequestCommand.cs`
- `Events/WalletCreditedIntegrationEvent.cs` (optional notification hook; built in Task 11)

Runtime (`Modules.Billing/`):
- `Domain/Wallet.cs`, `Domain/WalletTransaction.cs`, `Domain/TopupRequest.cs`
- `Data/Configurations/WalletConfiguration.cs`, `WalletTransactionConfiguration.cs`, `TopupRequestConfiguration.cs`
- `Features/v1/Wallets/GetMyWallet/{Query Handler,Endpoint}.cs`
- `Features/v1/Wallets/CreateTopupRequest/{Handler,Validator,Endpoint}.cs`
- `Features/v1/Wallets/GetMyTopupRequests/{Handler,Validator,Endpoint}.cs`
- `Features/v1/Wallets/GetTopupRequests/{Handler,Validator,Endpoint}.cs`
- `Features/v1/Wallets/ApproveTopupRequest/{Handler,Validator,Endpoint}.cs`
- `Features/v1/Wallets/RejectTopupRequest/{Handler,Validator,Endpoint}.cs`
- `Mappings/` — extend existing `ToDto()` mapping file(s) or add `WalletMappings.cs`.

**Backend — modified files**
- `Modules.Billing.Contracts/BillingEnums.cs` — add `InvoicePurpose.Topup = 2` + the two new enums.
- `Modules.Billing/Domain/Invoice.cs` — add a `CreateTopupDraft(...)` factory (or extend `CreateDraft`).
- `Modules.Billing/Data/BillingDbContext.cs` — 3 new `DbSet`s.
- `Modules.Billing/Data/Configurations/InvoiceConfiguration.cs` — filter the period-unique index to exclude `Topup`.
- `Modules.Billing/Services/BillingService.cs` + `Services/IBillingService.cs` — `GetOrCreateWalletAsync`, `CreateTopupInvoiceAsync`, and the wallet-credit branch inside `MarkInvoicePaidAsync`.
- `Modules.Billing/BillingModule.cs` — register the 6 new endpoints in `MapEndpoints`.
- `src/Host/FSH.Starter.Migrations.PostgreSQL/Billing/` — one new migration + updated snapshot.

**Frontend — admin (`clients/admin/`)**
- `src/api/wallet.ts` (new) — types + `listTopupRequests`, `approveTopupRequest`, `rejectTopupRequest`.
- `src/pages/billing/topups-list.tsx` (new)
- `src/pages/billing/layout.tsx`, `src/routes.tsx`, `src/lib/permissions.ts` (already has Billing perms), `src/components/layout/nav-items.ts` — wire route + nav tab.

**Frontend — dashboard (`clients/dashboard/`)**
- `src/api/wallet.ts` (new) — types + `getMyWallet`, `createTopupRequest`, `getMyTopupRequests`.
- `src/pages/wallet.tsx` (new)
- `src/routes.tsx`, `src/components/layout/nav-data.ts` — wire route + nav item.

**Tests**
- `src/Tests/Billing.Tests/...` (unit: domain + validators) and `src/Tests/Integration.Tests/...` (lifecycle + cross-tenant isolation), matching existing Billing test locations.

---

## Data model (reference for all tasks)

```
Wallet              (1 per tenant)
  Id            Guid (v7)
  TenantId      string(64)
  Currency      string(8)   default "USD"
  Balance       decimal(18,4)  -- = Σ WalletTransaction.Amount, kept denormalized for fast reads
  Status        WalletStatus  (Active=0, Frozen=1)
  CreatedAtUtc  DateTime
  UpdatedAtUtc  DateTime?
  UNIQUE(TenantId)

WalletTransaction   (append-only ledger)
  Id            Guid (v7)
  WalletId      Guid (FK -> Wallet, cascade)
  TenantId      string(64)            -- denormalized for root-gating queries
  Amount        decimal(18,4)         -- signed: +credit, -debit
  Kind          WalletTransactionKind (Topup=0, MessageCharge=1, Adjustment=2)
  Description   string(256)
  ReferenceId   string(128)?          -- e.g. TopupRequest Id or future message id
  CreatedAtUtc  DateTime
  INDEX(WalletId, CreatedAtUtc)
  INDEX(TenantId)

TopupRequest
  Id            Guid (v7)
  TenantId      string(64)
  Amount        decimal(18,4)
  Currency      string(8)
  Note          string(512)?
  Status        TopupRequestStatus (Pending=0, Invoiced=1, Completed=2, Rejected=3, Cancelled=4)
  InvoiceId     Guid?                 -- set on approve
  RequestedBy   string(64)?           -- user id
  DecisionNote  string(512)?          -- reject reason / approve note
  CreatedAtUtc  DateTime
  DecidedAtUtc  DateTime?
  CompletedAtUtc DateTime?
  INDEX(TenantId, Status)
  INDEX(InvoiceId)
```

**Lifecycle:** dashboard `CreateTopupRequest` → `Pending`. Admin `ApproveTopupRequest` → `BillingService.CreateTopupInvoiceAsync` makes a `Topup`-purpose invoice, **issues** it (existing `InvoiceIssued` email fires to the clinic), links `InvoiceId`, request → `Invoiced`. Clinic pays offline. Operator opens the **existing** invoice-detail page, clicks **Mark paid** → `BillingService.MarkInvoicePaidAsync` detects `Purpose == Topup`, writes a `+Topup` `WalletTransaction`, bumps `Wallet.Balance`, flips the request → `Completed`. Admin `RejectTopupRequest` → `Rejected` (only from `Pending`).

---

### Task 1: Enums + `InvoicePurpose.Topup`

**Files:**
- Modify: `src/Modules/Billing/Modules.Billing.Contracts/BillingEnums.cs`
- Test: `src/Tests/Billing.Tests/Domain/EnumOrderingTests.cs` (create)

**Interfaces:**
- Produces: `InvoicePurpose.Topup` (= 2); `WalletStatus { Active=0, Frozen=1 }`; `WalletTransactionKind { Topup=0, MessageCharge=1, Adjustment=2 }`; `TopupRequestStatus { Pending=0, Invoiced=1, Completed=2, Rejected=3, Cancelled=4 }`.

- [ ] **Step 1: Write the failing test** — pins the byte values so a future reorder (the EF default-omit footgun) breaks a test, not production.

`src/Tests/Billing.Tests/Domain/EnumOrderingTests.cs`:
```csharp
using FSH.Modules.Billing.Contracts;
using Shouldly;
using Xunit;

namespace FSH.Modules.Billing.Tests.Domain;

public sealed class EnumOrderingTests
{
    [Fact]
    public void InvoicePurpose_Topup_is_two()
        => ((int)InvoicePurpose.Topup).ShouldBe(2);

    [Fact]
    public void TopupRequestStatus_Pending_is_default_zero()
        => ((int)TopupRequestStatus.Pending).ShouldBe(0);

    [Fact]
    public void WalletStatus_Active_is_default_zero()
        => ((int)WalletStatus.Active).ShouldBe(0);

    [Fact]
    public void WalletTransactionKind_Topup_is_default_zero()
        => ((int)WalletTransactionKind.Topup).ShouldBe(0);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Tests/Billing.Tests/Billing.Tests.csproj --filter EnumOrderingTests`
Expected: FAIL — compile error, `Topup`/`WalletStatus`/etc. do not exist.

- [ ] **Step 3: Edit `BillingEnums.cs`** — add `Topup` to `InvoicePurpose` and append the three new enums. Keep the existing footgun comment.

```csharp
public enum InvoicePurpose
{
    // Usage=0 doubles as the column default (rows backfill to Usage). Do NOT reorder.
    Usage = 0,
    Subscription = 1,
    Topup = 2
}

public enum WalletStatus
{
    Active = 0,
    Frozen = 1
}

public enum WalletTransactionKind
{
    Topup = 0,
    MessageCharge = 1,
    Adjustment = 2
}

public enum TopupRequestStatus
{
    Pending = 0,
    Invoiced = 1,
    Completed = 2,
    Rejected = 3,
    Cancelled = 4
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/Tests/Billing.Tests/Billing.Tests.csproj --filter EnumOrderingTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Billing/Modules.Billing.Contracts/BillingEnums.cs src/Tests/Billing.Tests/Domain/EnumOrderingTests.cs
git commit -m "feat(billing): add Topup invoice purpose + wallet/topup enums"
```

---

### Task 2: `Wallet` + `WalletTransaction` domain entities

**Files:**
- Create: `src/Modules/Billing/Modules.Billing/Domain/Wallet.cs`
- Create: `src/Modules/Billing/Modules.Billing/Domain/WalletTransaction.cs`
- Test: `src/Tests/Billing.Tests/Domain/WalletTests.cs` (create)

**Interfaces:**
- Consumes: `WalletStatus`, `WalletTransactionKind` (Task 1); base types `AggregateRoot<Guid>` / `BaseEntity<Guid>` (existing, used by `Invoice`/`InvoiceLineItem`).
- Produces:
  - `Wallet.Create(string tenantId, string currency) : Wallet`
  - `Wallet.Credit(decimal amount, WalletTransactionKind kind, string description, string? referenceId) : WalletTransaction` — guards `amount > 0`, adds to `Balance`, returns the ledger row (Id set).
  - `Wallet.Debit(decimal amount, WalletTransactionKind kind, string description, string? referenceId) : WalletTransaction` — guards `0 < amount <= Balance`, subtracts.
  - Properties: `Guid Id`, `string TenantId`, `string Currency`, `decimal Balance`, `WalletStatus Status`, `DateTime CreatedAtUtc`, `DateTime? UpdatedAtUtc`, `IReadOnlyList<WalletTransaction> Transactions`.
  - `WalletTransaction`: `Guid Id`, `Guid WalletId`, `string TenantId`, `decimal Amount`, `WalletTransactionKind Kind`, `string Description`, `string? ReferenceId`, `DateTime CreatedAtUtc`.

- [ ] **Step 1: Write the failing test**

`src/Tests/Billing.Tests/Domain/WalletTests.cs`:
```csharp
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;
using Shouldly;
using Xunit;

namespace FSH.Modules.Billing.Tests.Domain;

public sealed class WalletTests
{
    [Fact]
    public void Create_starts_active_with_zero_balance()
    {
        var w = Wallet.Create("tenant-a", "USD");
        w.TenantId.ShouldBe("tenant-a");
        w.Currency.ShouldBe("USD");
        w.Balance.ShouldBe(0m);
        w.Status.ShouldBe(WalletStatus.Active);
        w.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Credit_increases_balance_and_returns_ledger_row()
    {
        var w = Wallet.Create("tenant-a", "USD");
        var tx = w.Credit(50m, WalletTransactionKind.Topup, "Top-up", "req-1");
        w.Balance.ShouldBe(50m);
        tx.Amount.ShouldBe(50m);
        tx.WalletId.ShouldBe(w.Id);
        tx.TenantId.ShouldBe("tenant-a");
        tx.ReferenceId.ShouldBe("req-1");
    }

    [Fact]
    public void Credit_rejects_non_positive_amount()
        => Should.Throw<ArgumentOutOfRangeException>(
            () => Wallet.Create("t", "USD").Credit(0m, WalletTransactionKind.Topup, "x", null));

    [Fact]
    public void Debit_beyond_balance_throws()
    {
        var w = Wallet.Create("tenant-a", "USD");
        w.Credit(10m, WalletTransactionKind.Topup, "Top-up", null);
        Should.Throw<InvalidOperationException>(
            () => w.Debit(25m, WalletTransactionKind.MessageCharge, "msg", null));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Tests/Billing.Tests/Billing.Tests.csproj --filter WalletTests`
Expected: FAIL — `Wallet` does not exist.

- [ ] **Step 3: Create `WalletTransaction.cs`**

```csharp
using FSH.Framework.Domain;
using FSH.Modules.Billing.Contracts;

namespace FSH.Modules.Billing.Domain;

public sealed class WalletTransaction : BaseEntity<Guid>
{
    public Guid WalletId { get; private set; }
    public string TenantId { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public WalletTransactionKind Kind { get; private set; }
    public string Description { get; private set; } = default!;
    public string? ReferenceId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private WalletTransaction() { }

    internal static WalletTransaction Create(
        Guid walletId, string tenantId, decimal amount,
        WalletTransactionKind kind, string description, string? referenceId)
        => new()
        {
            Id = Guid.CreateVersion7(),
            WalletId = walletId,
            TenantId = tenantId,
            Amount = amount,
            Kind = kind,
            Description = description,
            ReferenceId = referenceId,
            CreatedAtUtc = DateTime.UtcNow
        };
}
```
> Note: confirm the base-class namespace by opening `Invoice.cs`/`InvoiceLineItem.cs` — use the **same** `using` for `AggregateRoot<>`/`BaseEntity<>` they use. Adjust the `using FSH.Framework.Domain;` line to match.

- [ ] **Step 4: Create `Wallet.cs`**

```csharp
using FSH.Framework.Domain;
using FSH.Modules.Billing.Contracts;

namespace FSH.Modules.Billing.Domain;

public sealed class Wallet : AggregateRoot<Guid>
{
    private readonly List<WalletTransaction> _transactions = new();

    public string TenantId { get; private set; } = default!;
    public string Currency { get; private set; } = "USD";
    public decimal Balance { get; private set; }
    public WalletStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyList<WalletTransaction> Transactions => _transactions;

    private Wallet() { }

    public static Wallet Create(string tenantId, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new Wallet
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency,
            Balance = 0m,
            Status = WalletStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public WalletTransaction Credit(decimal amount, WalletTransactionKind kind, string description, string? referenceId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);
        var tx = WalletTransaction.Create(Id, TenantId, amount, kind, description, referenceId);
        _transactions.Add(tx);
        Balance += amount;
        UpdatedAtUtc = DateTime.UtcNow;
        return tx;
    }

    public WalletTransaction Debit(decimal amount, WalletTransactionKind kind, string description, string? referenceId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient wallet balance.");
        var tx = WalletTransaction.Create(Id, TenantId, -amount, kind, description, referenceId);
        _transactions.Add(tx);
        Balance -= amount;
        UpdatedAtUtc = DateTime.UtcNow;
        return tx;
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test src/Tests/Billing.Tests/Billing.Tests.csproj --filter WalletTests`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Billing/Modules.Billing/Domain/Wallet.cs src/Modules/Billing/Modules.Billing/Domain/WalletTransaction.cs src/Tests/Billing.Tests/Domain/WalletTests.cs
git commit -m "feat(billing): add Wallet aggregate + WalletTransaction ledger"
```

---

### Task 3: `TopupRequest` domain entity

**Files:**
- Create: `src/Modules/Billing/Modules.Billing/Domain/TopupRequest.cs`
- Test: `src/Tests/Billing.Tests/Domain/TopupRequestTests.cs` (create)

**Interfaces:**
- Consumes: `TopupRequestStatus` (Task 1).
- Produces:
  - `TopupRequest.Create(string tenantId, decimal amount, string currency, string? note, string? requestedBy) : TopupRequest` (Status=Pending).
  - `MarkInvoiced(Guid invoiceId, string? note)` — only from `Pending`, sets `InvoiceId`, `Status=Invoiced`, `DecidedAtUtc`.
  - `MarkCompleted()` — only from `Invoiced`, sets `Status=Completed`, `CompletedAtUtc`.
  - `Reject(string? reason)` — only from `Pending`.
  - Properties: `Guid Id`, `string TenantId`, `decimal Amount`, `string Currency`, `string? Note`, `TopupRequestStatus Status`, `Guid? InvoiceId`, `string? RequestedBy`, `string? DecisionNote`, `DateTime CreatedAtUtc`, `DateTime? DecidedAtUtc`, `DateTime? CompletedAtUtc`.

- [ ] **Step 1: Write the failing test**

`src/Tests/Billing.Tests/Domain/TopupRequestTests.cs`:
```csharp
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;
using Shouldly;
using Xunit;

namespace FSH.Modules.Billing.Tests.Domain;

public sealed class TopupRequestTests
{
    [Fact]
    public void Create_starts_pending()
    {
        var r = TopupRequest.Create("tenant-a", 50m, "USD", "need credit", "user-1");
        r.Status.ShouldBe(TopupRequestStatus.Pending);
        r.Amount.ShouldBe(50m);
        r.InvoiceId.ShouldBeNull();
    }

    [Fact]
    public void MarkInvoiced_from_pending_links_invoice()
    {
        var r = TopupRequest.Create("tenant-a", 50m, "USD", null, null);
        var inv = Guid.CreateVersion7();
        r.MarkInvoiced(inv, "approved");
        r.Status.ShouldBe(TopupRequestStatus.Invoiced);
        r.InvoiceId.ShouldBe(inv);
        r.DecidedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void MarkCompleted_requires_invoiced()
    {
        var r = TopupRequest.Create("tenant-a", 50m, "USD", null, null);
        Should.Throw<InvalidOperationException>(() => r.MarkCompleted());
    }

    [Fact]
    public void Reject_from_invoiced_throws()
    {
        var r = TopupRequest.Create("tenant-a", 50m, "USD", null, null);
        r.MarkInvoiced(Guid.CreateVersion7(), null);
        Should.Throw<InvalidOperationException>(() => r.Reject("late"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Tests/Billing.Tests/Billing.Tests.csproj --filter TopupRequestTests`
Expected: FAIL — `TopupRequest` does not exist.

- [ ] **Step 3: Create `TopupRequest.cs`**

```csharp
using FSH.Framework.Domain;
using FSH.Modules.Billing.Contracts;

namespace FSH.Modules.Billing.Domain;

public sealed class TopupRequest : AggregateRoot<Guid>
{
    public string TenantId { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string? Note { get; private set; }
    public TopupRequestStatus Status { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public string? RequestedBy { get; private set; }
    public string? DecisionNote { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DecidedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private TopupRequest() { }

    public static TopupRequest Create(string tenantId, decimal amount, string currency, string? note, string? requestedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amount, 0m);
        return new TopupRequest
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency,
            Note = note,
            RequestedBy = requestedBy,
            Status = TopupRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkInvoiced(Guid invoiceId, string? note)
    {
        Require(TopupRequestStatus.Pending);
        InvoiceId = invoiceId;
        DecisionNote = note;
        Status = TopupRequestStatus.Invoiced;
        DecidedAtUtc = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Require(TopupRequestStatus.Invoiced);
        Status = TopupRequestStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Reject(string? reason)
    {
        Require(TopupRequestStatus.Pending);
        DecisionNote = reason;
        Status = TopupRequestStatus.Rejected;
        DecidedAtUtc = DateTime.UtcNow;
    }

    private void Require(TopupRequestStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Top-up request must be {expected} (was {Status}).");
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/Tests/Billing.Tests/Billing.Tests.csproj --filter TopupRequestTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Billing/Modules.Billing/Domain/TopupRequest.cs src/Tests/Billing.Tests/Domain/TopupRequestTests.cs
git commit -m "feat(billing): add TopupRequest aggregate with status transitions"
```

---

### Task 4: EF configurations, DbSets, invoice-index filter, migration

**Files:**
- Create: `src/Modules/Billing/Modules.Billing/Data/Configurations/WalletConfiguration.cs`
- Create: `src/Modules/Billing/Modules.Billing/Data/Configurations/WalletTransactionConfiguration.cs`
- Create: `src/Modules/Billing/Modules.Billing/Data/Configurations/TopupRequestConfiguration.cs`
- Modify: `src/Modules/Billing/Modules.Billing/Data/BillingDbContext.cs`
- Modify: `src/Modules/Billing/Modules.Billing/Data/Configurations/InvoiceConfiguration.cs`
- Create: migration files under `src/Host/FSH.Starter.Migrations.PostgreSQL/Billing/`

**Interfaces:**
- Consumes: `Wallet`, `WalletTransaction`, `TopupRequest` (Tasks 2–3).
- Produces: `BillingDbContext.Wallets`, `.WalletTransactions`, `.TopupRequests` DbSets; DB tables `billing.Wallets`, `billing.WalletTransactions`, `billing.TopupRequests`; the invoice period-unique index now filtered to `Purpose <> 2`.

- [ ] **Step 1: Add DbSets to `BillingDbContext.cs`**

```csharp
public DbSet<Wallet> Wallets => Set<Wallet>();
public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
public DbSet<TopupRequest> TopupRequests => Set<TopupRequest>();
```
(Add `using FSH.Modules.Billing.Domain;` if not present.)

- [ ] **Step 2: Create `WalletConfiguration.cs`**

```csharp
using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Billing.Data.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Wallets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(8);
        builder.Property(x => x.Balance).HasPrecision(18, 4);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => x.TenantId).IsUnique().HasDatabaseName("ux_wallets_tenantid");

        builder.HasMany(x => x.Transactions)
            .WithOne()
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(Wallet.Transactions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
```

- [ ] **Step 3: Create `WalletTransactionConfiguration.cs`**

```csharp
using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Billing.Data.Configurations;

public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("WalletTransactions");
        builder.HasKey(x => x.Id);
        // Child reached only via Wallet.Transactions nav — pin Id generation or EF marks Modified, not Added.
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.Kind).HasConversion<int>();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(256);
        builder.Property(x => x.ReferenceId).HasMaxLength(128);
        builder.HasIndex(x => new { x.WalletId, x.CreatedAtUtc });
        builder.HasIndex(x => x.TenantId);
    }
}
```
> The `ValueGeneratedNever()` on `Id` is required because `WalletTransaction` is only ever added through the `Wallet.Transactions` collection (known EF footgun in this repo).

- [ ] **Step 4: Create `TopupRequestConfiguration.cs`**

```csharp
using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Billing.Data.Configurations;

public sealed class TopupRequestConfiguration : IEntityTypeConfiguration<TopupRequest>
{
    public void Configure(EntityTypeBuilder<TopupRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TopupRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(8);
        builder.Property(x => x.Note).HasMaxLength(512);
        builder.Property(x => x.DecisionNote).HasMaxLength(512);
        builder.Property(x => x.RequestedBy).HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => x.InvoiceId);
        builder.Ignore(x => x.DomainEvents);
    }
}
```

- [ ] **Step 5: Filter the invoice period-unique index** in `InvoiceConfiguration.cs` so multiple Topup invoices per period are allowed. Replace the existing `ux_invoices_tenant_period_purpose` index definition with:

```csharp
// Recurring invoices are unique per tenant/period/purpose; Topup invoices (Purpose=2) are
// ad-hoc and may repeat within a period, so exclude them from the uniqueness filter.
builder.HasIndex(x => new { x.TenantId, x.PeriodYear, x.PeriodMonth, x.Purpose })
    .IsUnique()
    .HasFilter($"\"Purpose\" <> {(int)Contracts.InvoicePurpose.Topup}")
    .HasDatabaseName("ux_invoices_tenant_period_purpose");
```

- [ ] **Step 6: Build to validate the model compiles**

Run: `dotnet build src/Modules/Billing/Modules.Billing/Modules.Billing.csproj`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 7: Add the migration** (full solution build first — `migrations remove`/`add` footgun).

Run:
```bash
dotnet build src/FSH.Starter.slnx
dotnet ef migrations add WhatsAppWalletTopup \
  --context BillingDbContext \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL/FSH.Starter.Migrations.PostgreSQL.csproj \
  --startup-project src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj \
  --output-dir Billing
```
Expected: new `*_WhatsAppWalletTopup.cs` + `.Designer.cs` under `Billing/`, updated `BillingDbContextModelSnapshot.cs`.

- [ ] **Step 8: Eyeball the migration** — confirm it `CreateTable`s `Wallets`, `WalletTransactions`, `TopupRequests` in schema `billing`, **and** drops+recreates `ux_invoices_tenant_period_purpose` with the `"Purpose" <> 2` filter. No unintended column drops elsewhere.

- [ ] **Step 9: Apply + smoke-test the migration** (Docker required).

Run: `dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply`
Expected: applies cleanly, no errors.

- [ ] **Step 10: Commit**

```bash
git add src/Modules/Billing/Modules.Billing/Data src/Host/FSH.Starter.Migrations.PostgreSQL/Billing
git commit -m "feat(billing): persist wallet/ledger/topup tables + filter invoice unique index"
```

---

### Task 5: DTOs + mappings

**Files:**
- Create: `src/Modules/Billing/Modules.Billing.Contracts/Dtos/WalletDto.cs`
- Create: `src/Modules/Billing/Modules.Billing.Contracts/Dtos/WalletTransactionDto.cs`
- Create: `src/Modules/Billing/Modules.Billing.Contracts/Dtos/TopupRequestDto.cs`
- Create: `src/Modules/Billing/Modules.Billing/Mappings/WalletMappings.cs`
- Test: `src/Tests/Billing.Tests/Mappings/WalletMappingTests.cs` (create)

**Interfaces:**
- Produces:
  - `WalletDto(Guid Id, string TenantId, string Currency, decimal Balance, string Status, DateTime CreatedAtUtc, IReadOnlyList<WalletTransactionDto> RecentTransactions)`
  - `WalletTransactionDto(Guid Id, decimal Amount, string Kind, string Description, string? ReferenceId, DateTime CreatedAtUtc)`
  - `TopupRequestDto(Guid Id, string TenantId, decimal Amount, string Currency, string? Note, string Status, Guid? InvoiceId, string? RequestedBy, string? DecisionNote, DateTime CreatedAtUtc, DateTime? DecidedAtUtc, DateTime? CompletedAtUtc)`
  - Extension methods `Wallet.ToDto(int recentCount = 10)`, `TopupRequest.ToDto()`, `WalletTransaction.ToDto()`.
- Note: `Status`/`Kind` are emitted as **strings** (`.ToString()`) to match the API's global string-enum policy.

- [ ] **Step 1: Write the failing test**

`src/Tests/Billing.Tests/Mappings/WalletMappingTests.cs`:
```csharp
using FSH.Modules.Billing.Domain;
using FSH.Modules.Billing.Mappings;
using FSH.Modules.Billing.Contracts;
using Shouldly;
using Xunit;

namespace FSH.Modules.Billing.Tests.Mappings;

public sealed class WalletMappingTests
{
    [Fact]
    public void Wallet_ToDto_emits_string_status_and_balance()
    {
        var w = Wallet.Create("tenant-a", "USD");
        w.Credit(50m, WalletTransactionKind.Topup, "Top-up", "req-1");
        var dto = w.ToDto();
        dto.Balance.ShouldBe(50m);
        dto.Status.ShouldBe("Active");
        dto.RecentTransactions.Count.ShouldBe(1);
        dto.RecentTransactions[0].Kind.ShouldBe("Topup");
    }

    [Fact]
    public void TopupRequest_ToDto_emits_string_status()
    {
        var r = TopupRequest.Create("tenant-a", 25m, "USD", "note", "u1");
        r.ToDto().Status.ShouldBe("Pending");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Tests/Billing.Tests/Billing.Tests.csproj --filter WalletMappingTests`
Expected: FAIL — DTOs/mappings missing.

- [ ] **Step 3: Create the three DTO records** (each in its own file under `Contracts/Dtos/`), matching the Produces signatures above. Example `WalletDto.cs`:

```csharp
namespace FSH.Modules.Billing.Contracts.Dtos;

public sealed record WalletDto(
    Guid Id,
    string TenantId,
    string Currency,
    decimal Balance,
    string Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<WalletTransactionDto> RecentTransactions);
```

- [ ] **Step 4: Create `WalletMappings.cs`**

```csharp
using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Domain;

namespace FSH.Modules.Billing.Mappings;

public static class WalletMappings
{
    public static WalletTransactionDto ToDto(this WalletTransaction t)
        => new(t.Id, t.Amount, t.Kind.ToString(), t.Description, t.ReferenceId, t.CreatedAtUtc);

    public static WalletDto ToDto(this Wallet w, int recentCount = 10)
        => new(
            w.Id, w.TenantId, w.Currency, w.Balance, w.Status.ToString(), w.CreatedAtUtc,
            w.Transactions
                .OrderByDescending(t => t.CreatedAtUtc)
                .Take(recentCount)
                .Select(t => t.ToDto())
                .ToList());

    public static TopupRequestDto ToDto(this TopupRequest r)
        => new(
            r.Id, r.TenantId, r.Amount, r.Currency, r.Note, r.Status.ToString(),
            r.InvoiceId, r.RequestedBy, r.DecisionNote, r.CreatedAtUtc, r.DecidedAtUtc, r.CompletedAtUtc);
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test src/Tests/Billing.Tests/Billing.Tests.csproj --filter WalletMappingTests`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Billing/Modules.Billing.Contracts/Dtos src/Modules/Billing/Modules.Billing/Mappings/WalletMappings.cs src/Tests/Billing.Tests/Mappings/WalletMappingTests.cs
git commit -m "feat(billing): wallet/topup DTOs + string-enum mappings"
```

---

### Task 6: `BillingService` wallet methods + credit-on-paid

**Files:**
- Modify: `src/Modules/Billing/Modules.Billing/Services/IBillingService.cs`
- Modify: `src/Modules/Billing/Modules.Billing/Services/BillingService.cs`
- Test: `src/Tests/Integration.Tests/Billing/WalletTopupServiceTests.cs` (create) — Testcontainers-backed.

**Interfaces:**
- Consumes: `BillingDbContext`, `Wallet`, `TopupRequest`, `Invoice.CreateDraft`/issue (existing), `InvoiceLineItemKind.Adjustment`.
- Produces (on `IBillingService`):
  - `Task<Wallet> GetOrCreateWalletAsync(string tenantId, string currency, CancellationToken ct = default)`
  - `Task<Invoice> CreateTopupInvoiceAsync(string tenantId, Guid topupRequestId, CancellationToken ct = default)` — loads the `Pending` request, creates a `Topup`-purpose Draft invoice with one `Adjustment` line item (`"WhatsApp wallet top-up"`, qty 1, unit = amount), **issues** it (fires existing `InvoiceIssuedIntegrationEvent`), calls `request.MarkInvoiced(invoice.Id, note)`, saves, returns the invoice.
  - **Inside existing `MarkInvoicePaidAsync`:** after the invoice is set Paid, if `invoice.Purpose == InvoicePurpose.Topup`, find the `TopupRequest` by `InvoiceId`, `GetOrCreateWalletAsync`, `wallet.Credit(invoice.SubtotalAmount, WalletTransactionKind.Topup, "WhatsApp wallet top-up", request.Id.ToString())`, `request.MarkCompleted()`, persist — all in the same `SaveChangesAsync`.

- [ ] **Step 1: Write the failing integration test**

`src/Tests/Integration.Tests/Billing/WalletTopupServiceTests.cs` (follow the existing Billing integration-test harness: same base class/fixture as the current invoice integration tests — open a sibling file to copy the `[Collection]`/fixture attributes and the tenant-context setup):
```csharp
// Pattern note: set the Finbuckle tenant context INLINE in this method (AsyncLocal is lost across awaited helpers).
[Fact]
public async Task Topup_credits_wallet_when_invoice_marked_paid()
{
    // arrange: resolve IBillingService + BillingDbContext from the test host; set tenant context to a seeded tenant inline.
    var request = TopupRequest.Create(TenantId, 50m, "USD", "need credit", "user-1");
    Db.TopupRequests.Add(request);
    await Db.SaveChangesAsync();

    // act
    var invoice = await Billing.CreateTopupInvoiceAsync(TenantId, request.Id);
    invoice.Purpose.ShouldBe(InvoicePurpose.Topup);
    invoice.Status.ShouldBe(InvoiceStatus.Issued);

    await Billing.MarkInvoicePaidAsync(invoice.Id);

    // assert
    var wallet = await Billing.GetOrCreateWalletAsync(TenantId, "USD");
    wallet.Balance.ShouldBe(50m);
    var reloaded = await Db.TopupRequests.FindAsync(request.Id);
    reloaded!.Status.ShouldBe(TopupRequestStatus.Completed);
}
```

- [ ] **Step 2: Run test to verify it fails** (Docker required)

Run: `dotnet test src/Tests/Integration.Tests/Integration.Tests.csproj --filter WalletTopupServiceTests`
Expected: FAIL — `CreateTopupInvoiceAsync`/`GetOrCreateWalletAsync` not defined.

- [ ] **Step 3: Add the three signatures to `IBillingService.cs`** (per the Produces block).

- [ ] **Step 4: Implement in `BillingService.cs`.** Add (using the existing `_db`, `_eventBus`, `_timeProvider` fields — confirm their names by opening the file):

```csharp
public async Task<Wallet> GetOrCreateWalletAsync(string tenantId, string currency, CancellationToken ct = default)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
    var wallet = await _db.Wallets
        .Include(w => w.Transactions)
        .FirstOrDefaultAsync(w => w.TenantId == tenantId, ct)
        .ConfigureAwait(false);
    if (wallet is null)
    {
        wallet = Wallet.Create(tenantId, currency);
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    return wallet;
}

public async Task<Invoice> CreateTopupInvoiceAsync(string tenantId, Guid topupRequestId, CancellationToken ct = default)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
    var request = await _db.TopupRequests
        .FirstOrDefaultAsync(r => r.Id == topupRequestId && r.TenantId == tenantId, ct)
        .ConfigureAwait(false)
        ?? throw new NotFoundException($"Top-up request {topupRequestId} not found.");

    var now = _timeProvider.GetUtcNow().UtcDateTime;
    var invoice = Invoice.CreateTopupDraft(tenantId, request.Currency, now, request.Amount,
        $"WhatsApp wallet top-up ({request.Amount:0.##} {request.Currency})");
    invoice.Issue();
    _db.Invoices.Add(invoice);
    request.MarkInvoiced(invoice.Id, request.Note);

    await _db.SaveChangesAsync(ct).ConfigureAwait(false);

    await _eventBus.PublishAsync(new InvoiceIssuedIntegrationEvent(
        Id: Guid.NewGuid(),
        OccurredOnUtc: now,
        TenantId: tenantId,
        CorrelationId: Guid.NewGuid().ToString(),
        Source: "Billing",
        InvoiceId: invoice.Id,
        InvoiceNumber: invoice.InvoiceNumber,
        Amount: invoice.SubtotalAmount,
        Currency: invoice.Currency,
        DueAtUtc: invoice.DueAtUtc,
        PeriodYear: invoice.PeriodYear,
        PeriodMonth: invoice.PeriodMonth), ct).ConfigureAwait(false);

    return invoice;
}
```
And inside the existing `MarkInvoicePaidAsync`, after the invoice is marked paid and **before** the final `SaveChangesAsync`, add the topup branch:
```csharp
if (invoice.Purpose == InvoicePurpose.Topup)
{
    var request = await _db.TopupRequests
        .FirstOrDefaultAsync(r => r.InvoiceId == invoice.Id, ct).ConfigureAwait(false);
    if (request is { Status: TopupRequestStatus.Invoiced })
    {
        var wallet = await _db.Wallets
            .FirstOrDefaultAsync(w => w.TenantId == invoice.TenantId, ct).ConfigureAwait(false);
        if (wallet is null)
        {
            wallet = Wallet.Create(invoice.TenantId, invoice.Currency);
            _db.Wallets.Add(wallet);
        }
        wallet.Credit(invoice.SubtotalAmount, WalletTransactionKind.Topup,
            "WhatsApp wallet top-up", request.Id.ToString());
        request.MarkCompleted();
    }
}
```
> If `MarkInvoicePaidAsync` currently loads the invoice with a narrow projection, ensure it loads the tracked `Invoice` entity (with `Purpose`, `TenantId`, `SubtotalAmount`) so this branch works in the same unit of work.

- [ ] **Step 5: Add `Invoice.CreateTopupDraft` factory** in `Invoice.cs` (mirror `CreateDraft`; sets `Purpose = InvoicePurpose.Topup`, `PeriodYear`/`PeriodMonth` from `now`, adds one `Adjustment` line item via the existing line-item path, sets `SubtotalAmount = amount`). Confirm the existing private line-item add helper name and reuse it.

- [ ] **Step 6: Run test to verify it passes** (Docker required)

Run: `dotnet test src/Tests/Integration.Tests/Integration.Tests.csproj --filter WalletTopupServiceTests`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/Modules/Billing/Modules.Billing/Services src/Modules/Billing/Modules.Billing/Domain/Invoice.cs src/Tests/Integration.Tests/Billing/WalletTopupServiceTests.cs
git commit -m "feat(billing): topup invoice generation + wallet credit on invoice paid"
```

---

### Task 7: Tenant-facing commands/queries + endpoints (dashboard side)

**Files:**
- Create contracts: `v1/Wallets/GetMyWalletQuery.cs`, `v1/Wallets/CreateTopupRequestCommand.cs`, `v1/Wallets/GetMyTopupRequestsQuery.cs` (under `Modules.Billing.Contracts/`)
- Create handlers/validators/endpoints under `Modules.Billing/Features/v1/Wallets/{GetMyWallet,CreateTopupRequest,GetMyTopupRequests}/`
- Modify: `src/Modules/Billing/Modules.Billing/BillingModule.cs` (register 3 endpoints)
- Test: `src/Tests/Billing.Tests/Validators/CreateTopupRequestValidatorTests.cs` + `src/Tests/Integration.Tests/Billing/WalletEndpointsTests.cs`

**Interfaces:**
- Consumes: `IBillingService`, `BillingDbContext`, `IMultiTenantContextAccessor<AppTenantInfo>`, `ICurrentUser` (for `RequestedBy`; confirm the accessor used elsewhere), `WalletMappings`.
- Produces routes (all under `api/v{version}/billing`, `RequirePermission(BillingPermissions.View)`):
  - `GET  /wallet/me` → `WalletDto`
  - `POST /wallet/topup-requests` (body `{ amount, note }`) → `Guid`
  - `GET  /wallet/topup-requests/me?status=&pageNumber=&pageSize=` → `PagedResponse<TopupRequestDto>`

- [ ] **Step 1: Write the failing validator test**

`src/Tests/Billing.Tests/Validators/CreateTopupRequestValidatorTests.cs`:
```csharp
using FSH.Modules.Billing.Features.v1.Wallets.CreateTopupRequest;
using FSH.Modules.Billing.Contracts.v1.Wallets;
using Shouldly;
using Xunit;

namespace FSH.Modules.Billing.Tests.Validators;

public sealed class CreateTopupRequestValidatorTests
{
    private readonly CreateTopupRequestCommandValidator _v = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1_000_001)]
    public void Rejects_out_of_range(decimal amount)
        => _v.Validate(new CreateTopupRequestCommand(amount, null)).IsValid.ShouldBeFalse();

    [Fact]
    public void Accepts_valid_amount()
        => _v.Validate(new CreateTopupRequestCommand(50m, "need credit")).IsValid.ShouldBeTrue();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Tests/Billing.Tests/Billing.Tests.csproj --filter CreateTopupRequestValidatorTests`
Expected: FAIL — types missing.

- [ ] **Step 3: Create the contracts.**

`v1/Wallets/CreateTopupRequestCommand.cs`:
```csharp
using Mediator;

namespace FSH.Modules.Billing.Contracts.v1.Wallets;

public sealed record CreateTopupRequestCommand(decimal Amount, string? Note) : ICommand<Guid>;
```
`v1/Wallets/GetMyWalletQuery.cs`:
```csharp
using FSH.Modules.Billing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Billing.Contracts.v1.Wallets;

public sealed record GetMyWalletQuery : IQuery<WalletDto>;
```
`v1/Wallets/GetMyTopupRequestsQuery.cs`:
```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Billing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Billing.Contracts.v1.Wallets;

public sealed record GetMyTopupRequestsQuery(
    TopupRequestStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedResponse<TopupRequestDto>>;
```

- [ ] **Step 4: Create the validator** `Features/v1/Wallets/CreateTopupRequest/CreateTopupRequestCommandValidator.cs`:
```csharp
using FluentValidation;
using FSH.Modules.Billing.Contracts.v1.Wallets;

namespace FSH.Modules.Billing.Features.v1.Wallets.CreateTopupRequest;

public sealed class CreateTopupRequestCommandValidator : AbstractValidator<CreateTopupRequestCommand>
{
    public CreateTopupRequestCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0m).LessThanOrEqualTo(1_000_000m);
        RuleFor(x => x.Note).MaximumLength(512);
    }
}
```
Add a `GetMyTopupRequestsQueryValidator` (paginated query → validator required):
```csharp
using FluentValidation;
using FSH.Modules.Billing.Contracts.v1.Wallets;

namespace FSH.Modules.Billing.Features.v1.Wallets.GetMyTopupRequests;

public sealed class GetMyTopupRequestsQueryValidator : AbstractValidator<GetMyTopupRequestsQuery>
{
    public GetMyTopupRequestsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
```

- [ ] **Step 5: Run validator test to verify it passes**

Run: `dotnet test src/Tests/Billing.Tests/Billing.Tests.csproj --filter CreateTopupRequestValidatorTests`
Expected: PASS.

- [ ] **Step 6: Create the handlers** (root-gate to caller's own tenant — non-root callers only ever see their own data; root has no implicit tenant here so `GetMyWallet`/create resolve the caller's tenant id).

`CreateTopupRequestCommandHandler.cs`:
```csharp
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts.v1.Wallets;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Mediator;

namespace FSH.Modules.Billing.Features.v1.Wallets.CreateTopupRequest;

public sealed class CreateTopupRequestCommandHandler(
    BillingDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    ICurrentUser currentUser)        // confirm the ICurrentUser type/namespace used in this module
    : ICommandHandler<CreateTopupRequestCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateTopupRequestCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.");
        var currency = tenantAccessor.MultiTenantContext?.TenantInfo is { } t ? "USD" : "USD"; // wallet currency default; refine later
        var request = TopupRequest.Create(tenantId, command.Amount, currency, command.Note, currentUser.GetUserId()?.ToString());
        db.TopupRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return request.Id;
    }
}
```
`GetMyWalletQueryHandler.cs` — resolve tenant, `await billing.GetOrCreateWalletAsync(tenantId, "USD", ct)`, return `wallet.ToDto()`.
`GetMyTopupRequestsQueryHandler.cs` — resolve tenant, query `db.TopupRequests.AsNoTracking().Where(r => r.TenantId == tenantId)` + optional status filter + paginate (mirror `GetInvoicesQueryHandler` pagination), map `ToDto()`.

- [ ] **Step 7: Create the three endpoints** mirroring `GetInvoicesEndpoint`/`MarkInvoicePaidEndpoint` (use `RequirePermission(BillingPermissions.View)`; `.WithIdempotency()` on the POST). Register them in `BillingModule.MapEndpoints`:
```csharp
group.MapGetMyWalletEndpoint();              // GET  /wallet/me
group.MapCreateTopupRequestEndpoint();       // POST /wallet/topup-requests
group.MapGetMyTopupRequestsEndpoint();       // GET  /wallet/topup-requests/me
```

- [ ] **Step 8: Write the integration test** `WalletEndpointsTests.cs`: as a tenant user, POST a topup request → 200 + id; GET `/wallet/topup-requests/me` shows it `Pending`; GET `/wallet/me` shows balance 0. **Cross-tenant:** a request created by tenant A is **not** visible in tenant B's `/wallet/topup-requests/me`.

- [ ] **Step 9: Run the integration test** (Docker required)

Run: `dotnet test src/Tests/Integration.Tests/Integration.Tests.csproj --filter WalletEndpointsTests`
Expected: PASS.

- [ ] **Step 10: Commit**

```bash
git add src/Modules/Billing/Modules.Billing.Contracts/v1/Wallets src/Modules/Billing/Modules.Billing/Features/v1/Wallets/GetMyWallet src/Modules/Billing/Modules.Billing/Features/v1/Wallets/CreateTopupRequest src/Modules/Billing/Modules.Billing/Features/v1/Wallets/GetMyTopupRequests src/Modules/Billing/Modules.Billing/BillingModule.cs src/Tests
git commit -m "feat(billing): tenant wallet + topup-request endpoints"
```

---

### Task 8: Operator commands + endpoints (admin side)

**Files:**
- Create contracts: `v1/Wallets/GetTopupRequestsQuery.cs`, `v1/Wallets/ApproveTopupRequestCommand.cs`, `v1/Wallets/RejectTopupRequestCommand.cs`
- Create handlers/validators/endpoints under `Features/v1/Wallets/{GetTopupRequests,ApproveTopupRequest,RejectTopupRequest}/`
- Modify: `BillingModule.cs` (register 3 endpoints)
- Test: `src/Tests/Integration.Tests/Billing/TopupApprovalTests.cs`

**Interfaces:**
- Consumes: `IBillingService.CreateTopupInvoiceAsync` (Task 6), root-gating pattern.
- Produces routes under `api/v{version}/billing`:
  - `GET  /wallet/topup-requests?tenantId=&status=&pageNumber=&pageSize=` → `PagedResponse<TopupRequestDto>` — `RequirePermission(BillingPermissions.View)`, **root-only cross-tenant** (mirror `GetInvoicesQueryHandler`: non-root scoped to own tenant).
  - `POST /wallet/topup-requests/{id}/approve` (body `{ note? }`) → `Guid` (the created invoice id) — `RequirePermission(BillingPermissions.Manage)`, `.WithIdempotency()`.
  - `POST /wallet/topup-requests/{id}/reject` (body `{ reason? }`) → `Guid` — `RequirePermission(BillingPermissions.Manage)`, `.WithIdempotency()`.

- [ ] **Step 1: Write the failing integration test**

`TopupApprovalTests.cs` (root operator):
```csharp
[Fact]
public async Task Approve_generates_topup_invoice_and_marks_request_invoiced()
{
    // arrange: seed a Pending TopupRequest for TenantA (inline tenant context for the seed).
    // act: as ROOT, POST /api/v1/billing/wallet/topup-requests/{id}/approve
    // assert: 200 + invoiceId; request now Invoiced with InvoiceId set; an Issued Topup invoice exists for TenantA.
}

[Fact]
public async Task NonRoot_cannot_see_other_tenants_requests()
{
    // arrange: Pending request for TenantA.
    // act: as TenantB user, GET /api/v1/billing/wallet/topup-requests
    // assert: TenantA's request is absent.
}
```

- [ ] **Step 2: Run test to verify it fails** (Docker)

Run: `dotnet test src/Tests/Integration.Tests/Integration.Tests.csproj --filter TopupApprovalTests`
Expected: FAIL — endpoints missing.

- [ ] **Step 3: Create contracts** `GetTopupRequestsQuery(string? TenantId, TopupRequestStatus? Status, int PageNumber=1, int PageSize=20) : IQuery<PagedResponse<TopupRequestDto>>`; `ApproveTopupRequestCommand(Guid Id, string? Note) : ICommand<Guid>`; `RejectTopupRequestCommand(Guid Id, string? Reason) : ICommand<Guid>`.

- [ ] **Step 4: Create validators** — `GetTopupRequestsQueryValidator` (PageNumber>0, PageSize 1..100), `ApproveTopupRequestCommandValidator` (`RuleFor(x=>x.Id).NotEmpty()`), `RejectTopupRequestCommandValidator` (`Id` NotEmpty, `Reason` MaxLength 512).

- [ ] **Step 5: Create handlers.**
  - `GetTopupRequestsQueryHandler` — root-gate (copy the `callerTenantId`/`isRoot`/`tenantFilter` block from `GetInvoicesQueryHandler`), filter by `tenantFilter`/`Status`, paginate, `ToDto()`.
  - `ApproveTopupRequestCommandHandler` — resolve caller tenant; if root, the request's own `TenantId` is the scope (load request by id, use `request.TenantId`); else require `request.TenantId == callerTenantId`. Call `await billing.CreateTopupInvoiceAsync(request.TenantId, command.Id, ct)`; return `invoice.Id`.
  - `RejectTopupRequestCommandHandler` — load request (same root-gate), `request.Reject(reason)`, save, return `request.Id`.

- [ ] **Step 6: Create endpoints + register** in `BillingModule.MapEndpoints`:
```csharp
group.MapGetTopupRequestsEndpoint();          // GET  /wallet/topup-requests
group.MapApproveTopupRequestEndpoint();       // POST /wallet/topup-requests/{id}/approve
group.MapRejectTopupRequestEndpoint();        // POST /wallet/topup-requests/{id}/reject
```

- [ ] **Step 7: Run test to verify it passes** (Docker)

Run: `dotnet test src/Tests/Integration.Tests/Integration.Tests.csproj --filter TopupApprovalTests`
Expected: PASS.

- [ ] **Step 8: Full backend gate**

Run: `dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj`
Expected: build green (TreatWarningsAsErrors), architecture tests pass (every command/paginated-query handler has a validator; module boundaries intact).

- [ ] **Step 9: Commit**

```bash
git add src/Modules/Billing src/Tests
git commit -m "feat(billing): operator list/approve/reject topup-request endpoints"
```

---

### Task 9: Dashboard UI — wallet + request top-up

**Files:**
- Create: `clients/dashboard/src/api/wallet.ts`
- Create: `clients/dashboard/src/pages/wallet.tsx`
- Modify: `clients/dashboard/src/routes.tsx`, `clients/dashboard/src/components/layout/nav-data.ts`

**Interfaces:**
- Consumes: `apiFetch`, `PagedResult<T>` (existing in `clients/dashboard/src/api/billing.ts` — import the type or re-declare consistently).
- Produces (in `api/wallet.ts`):
  - types `WalletDto`, `WalletTransactionDto`, `TopupRequestStatus = "Pending" | "Invoiced" | "Completed" | "Rejected" | "Cancelled" | (string & {})`, `TopupRequestDto`.
  - `getMyWallet(): Promise<WalletDto>` → `GET /api/v1/billing/wallet/me`
  - `createTopupRequest(input: { amount: number; note?: string }): Promise<string>` → `POST /api/v1/billing/wallet/topup-requests`
  - `getMyTopupRequests(params?: { status?: TopupRequestStatus; pageNumber?: number; pageSize?: number }): Promise<PagedResult<TopupRequestDto>>` → `GET /api/v1/billing/wallet/topup-requests/me`

- [ ] **Step 1: Create `api/wallet.ts`** — mirror `clients/dashboard/src/api/billing.ts` exactly (same `apiFetch` import, `URLSearchParams` query building, string-union enums).

- [ ] **Step 2: Create `pages/wallet.tsx`** — plain controlled `useState` form (dashboard convention, no rhf/zod). Structure:
  - `useQuery(["billing","wallet","me"], getMyWallet)` → balance card (big `Balance` + `Currency`, low-balance hint).
  - "Request top-up" card: controlled `amount` (number string) + `note` (textarea); inline `valid = Number(amount) > 0`; `useMutation({ mutationFn: createTopupRequest })` — pass the value via `mutate({ amount: Number(amount), note })` (never via closed-over state); `onSuccess` → toast + `invalidateQueries(["billing","topup-requests","me"])` + reset fields.
  - `useQuery(["billing","topup-requests","me", {page}], () => getMyTopupRequests({pageNumber}))` → list with status badges (`Pending`=amber, `Invoiced`=blue, `Completed`=green, `Rejected`=red) and amount; if `invoiceId` present, link to `/invoices/{invoiceId}`.
  - Use the same page-header/card/skeleton components the existing `pages/invoices.tsx` imports.

- [ ] **Step 3: Register route + nav.** In `routes.tsx`:
```tsx
const WalletPage = lazyNamed(() => import("@/pages/wallet"), "WalletPage");
// ...
{ path: "/wallet", element: <WalletPage /> },
```
In `components/layout/nav-data.ts`, add under the "operations" group (after Subscription):
```ts
{ to: "/wallet", label: "WhatsApp wallet", icon: Wallet, perm: "Permissions.Billing.View" },
```
(Import `Wallet` from `lucide-react`.)

- [ ] **Step 4: Typecheck + build the dashboard**

Run: `cd clients/dashboard && npm run build`
Expected: type-checks and builds with no errors.

- [ ] **Step 5: Commit**

```bash
git add clients/dashboard/src/api/wallet.ts clients/dashboard/src/pages/wallet.tsx clients/dashboard/src/routes.tsx clients/dashboard/src/components/layout/nav-data.ts
git commit -m "feat(dashboard): WhatsApp wallet page + request top-up"
```

---

### Task 10: Admin UI — top-up requests review/approve

**Files:**
- Create: `clients/admin/src/api/wallet.ts`
- Create: `clients/admin/src/pages/billing/topups-list.tsx`
- Modify: `clients/admin/src/pages/billing/layout.tsx` (add tab), `clients/admin/src/routes.tsx`, `clients/admin/src/components/layout/nav-items.ts` (Billing nav already present; no change needed unless adding a sub-item)

**Interfaces:**
- Consumes: `apiFetch`, `PagedResponse<T>` (`@/lib/api-types`), `BillingPermissions` (`@/lib/permissions`), the existing invoice-detail route `/billing/invoices/:invoiceId` (approve links the operator there to mark paid).
- Produces (in `api/wallet.ts`):
  - types `TopupRequestStatus` union + `TopupRequestDto` (same shape as dashboard).
  - `listTopupRequests(params: { tenantId?: string; status?: TopupRequestStatus; pageNumber?: number; pageSize?: number }): Promise<PagedResponse<TopupRequestDto>>` → `GET /api/v1/billing/wallet/topup-requests`
  - `approveTopupRequest(id: string, note?: string): Promise<string>` → `POST /api/v1/billing/wallet/topup-requests/{id}/approve`
  - `rejectTopupRequest(id: string, reason?: string): Promise<string>` → `POST /api/v1/billing/wallet/topup-requests/{id}/reject`

- [ ] **Step 1: Create `api/wallet.ts`** — mirror `clients/admin/src/api/billing.ts` (same imports/helpers/string-union enums).

- [ ] **Step 2: Create `pages/billing/topups-list.tsx`** — clone the structure of `pages/billing/invoices-list.tsx`:
  - filters: `tenantId` (Input), `status` (Select, default `Pending`), pagination state.
  - `useQuery(["billing","topup-requests",{...}], () => listTopupRequests({...}), { placeholderData: keepPreviousData })`.
  - KPI tiles: pending count, total requested on page.
  - rows: tenant id, amount+currency, status badge, created date; if `invoiceId`, a link to `/billing/invoices/{invoiceId}`.
  - per-row actions gated by `canManageBilling = currentUser.permissions.includes(BillingPermissions.Manage)`:
    - **Approve** → `useMutation({ mutationFn: (id) => approveTopupRequest(id, note) })`; on success toast "Invoice generated", `invalidateQueries(["billing","topup-requests"])`, and offer a link to the new invoice id returned.
    - **Reject** → confirm + reason; `useMutation` → `rejectTopupRequest`. (Avoid native `confirm()` dialogs per browser-automation note; use the app's existing dialog/AlertDialog component as invoices/other admin pages do.)
  - Pass per-call data via `mutate(arg)`.

- [ ] **Step 3: Register route + tab.** In `routes.tsx` billing children:
```tsx
const TopupsListPage = lazyNamed(() => import("@/pages/billing/topups-list"), "TopupsListPage");
// ...
{ path: "topups", element: <TopupsListPage /> },
```
Add a "Top-ups" tab to `pages/billing/layout.tsx` alongside Invoices/Plans (follow the existing tab list pattern in that file).

- [ ] **Step 4: Typecheck + build the admin app**

Run: `cd clients/admin && npm run build`
Expected: type-checks and builds with no errors.

- [ ] **Step 5: Commit**

```bash
git add clients/admin/src/api/wallet.ts clients/admin/src/pages/billing/topups-list.tsx clients/admin/src/pages/billing/layout.tsx clients/admin/src/routes.tsx
git commit -m "feat(admin): top-up requests review/approve/reject page"
```

---

### Task 11: Frontend E2E (Playwright, route-mocked)

**Files:**
- Create: `clients/admin/tests/billing/topups.spec.ts`
- Create: `clients/dashboard/tests/wallet.spec.ts`

**Interfaces:**
- Consumes: the existing Playwright harness (JWT-seeded localStorage + route mocking). Copy the setup from a sibling billing spec (`clients/admin/tests/billing/*.spec.ts`, `clients/dashboard/tests/invoices.spec.ts`).

- [ ] **Step 1: Dashboard spec** — mock `GET /api/v1/billing/wallet/me` (balance) + `GET .../topup-requests/me` (one Pending) + `POST .../topup-requests` (200 + id). Assert: balance renders; submitting the form fires the POST with the typed amount; the list refetch shows the request. **Mock every on-open endpoint** the page calls (an unmocked call 401s → auto-logout flake).

- [ ] **Step 2: Admin spec** — seed an operator JWT with `Permissions.Billing.Manage`; mock `GET .../wallet/topup-requests` (one Pending) + `POST .../{id}/approve` (200 + invoiceId). Assert: the request renders; clicking Approve fires the POST and shows the success toast / invoice link.

- [ ] **Step 3: Run both suites**

Run: `cd clients/admin && npx playwright test billing/topups` then `cd clients/dashboard && npx playwright test wallet`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add clients/admin/tests/billing/topups.spec.ts clients/dashboard/tests/wallet.spec.ts
git commit -m "test(e2e): wallet topup request + admin approval flows"
```

---

### Task 12: Docs + changelog (definition of done)

**Files:**
- Modify (docs repo `C:\Users\mukesh\repos\fullstackhero\docs`): a Billing/WhatsApp wallet page describing the top-up lifecycle + the BSP model.
- Create: `src/content/docs/changelog/<entry>.md` (in the docs repo's changelog dir; match the existing entry format).

**Interfaces:** none (documentation).

- [ ] **Step 1: Add changelog entry** in the docs repo describing: prepaid WhatsApp wallet, dashboard top-up request, admin approve→invoice→mark-paid→auto-credit, manual offline payment. Note Phase 2 (metering) is not yet shipped.

- [ ] **Step 2: Add/Update the docs page** — explain Model B (we are the BSP, Meta bills us, clinics top up with us), the wallet money model, and the operator workflow. Cross-link from the Billing docs.

- [ ] **Step 3: Commit (in the docs repo)**

```bash
git -C C:/Users/mukesh/repos/fullstackhero/docs add .
git -C C:/Users/mukesh/repos/fullstackhero/docs commit -m "docs: WhatsApp prepaid wallet + top-up lifecycle"
```

---

## Out of scope (Phase 2 — do NOT build here)

- **Metering / debit:** decrementing the wallet per WhatsApp template send (per Meta category pricing + markup). Requires the Meta Cloud API send integration, which does not exist yet. `Wallet.Debit` + `WalletTransactionKind.MessageCharge` are built now so the ledger is ready, but nothing calls `Debit` in Phase 1.
- **Low-balance notifications / auto-block** at zero balance.
- **Fixed top-up packages** (UI presets) — Phase 1 uses arbitrary amount with a min/max validator.
- **Per-category credit pricing display** ("messages remaining") — Phase 1 shows a money balance.
- **Embedded Signup / WABA onboarding** under our Meta Tech-Provider account.

---

## Self-Review

**Spec coverage** (against the agreed design): tenant requests top-up → Tasks 7, 9. Admin sees requests → Tasks 8, 10. Admin generates invoice + approves → Tasks 6, 8, 10. Manual payment → reuses existing invoice mark-paid UI. Wallet credited on paid → Task 6. Money-amount wallet → Tasks 2, 4, 5. Credit only on paid → Task 6 (branch inside `MarkInvoicePaidAsync`). Cross-tenant isolation → Tasks 7, 8 (root-gating + tests). Migration → Task 4. Docs/changelog → Task 12. No gaps.

**Placeholder scan:** every code step shows real code; the two intentional "confirm the exact name in the existing file" notes (base-class `using`, `ICurrentUser` accessor, `_db`/`_eventBus`/`_timeProvider` field names, the private line-item add helper) are verification instructions, not deferred work — the implementer confirms a name, not designs a behavior.

**Type consistency:** `TopupRequestStatus` members (`Pending/Invoiced/Completed/Rejected/Cancelled`) are identical across enum (Task 1), domain transitions (Task 3), DTO string output (Task 5), and both frontends (Tasks 9, 10). `WalletTransactionKind.Topup` used consistently in credit calls (Tasks 2, 6). `CreateTopupInvoiceAsync`/`GetOrCreateWalletAsync`/`MarkInvoicePaidAsync` signatures match between Task 6 (definition) and Tasks 7–8 (callers). Route paths (`/wallet/me`, `/wallet/topup-requests`, `/wallet/topup-requests/me`, `/wallet/topup-requests/{id}/approve|reject`) match between endpoint registration (Tasks 7–8) and frontend API modules (Tasks 9–10).

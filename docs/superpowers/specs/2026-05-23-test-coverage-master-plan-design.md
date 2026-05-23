# Master Test Plan — Risk-Tiered Coverage & Enforcement

**Date:** 2026-05-23
**Status:** Design (sub-project A of 3)
**Author:** brainstormed with Mukesh

## Goal

Establish a *permanent, demonstrable* feature-coverage safety net for the starter kit — not a one-time push. The test suite is part of the product: forkers learn how to test from these examples, so coverage must be principled (risk-tiered), self-maintaining (enforced in CI), and free of redundant tests.

This is the foundation artifact. It defines what "covered" means, audits the existing ~390 integration tests into a matrix, and produces the prioritized gap backlog that drives sub-projects B and C.

## Non-goals

- Literal line/branch 100%. We measure *behaviors proven*, not lines executed.
- Duplicating scenarios already covered. A matrix cell is `covered` (leave it), `thin` (harden in place), or `gap` (fill). Never add a second test for a `covered` cell.
- Rewriting the existing suite. It is strong; we harden thin spots and fill holes.

## Sub-project decomposition

| # | Sub-project | Output | Depends on |
|---|-------------|--------|-----------|
| **A** | Master Test Plan (this doc) | Risk model + coverage matrix + gap backlog + enforcement design | — |
| **B** | Backend gap-fill + Frontend E2E | New/hardened integration tests (this doc's backlog) + Playwright suites for admin & dashboard | A |
| **C** | CI coverage ratchet | Coverage collection + Codecov + trait-aggregator gate + frontend CI job | A, B producing coverage |

Each gets its own spec → plan → implement cycle. This doc fully specs A and scopes B/C.

## Risk model

Effort follows blast radius. Each feature lands in one tier; each tier mandates a set of scenario classes.

| Tier | Scope | Required scenarios (backend) | Frontend E2E |
|------|-------|------------------------------|--------------|
| **T1 — Critical** | security / tenant / money / identity | Happy · AuthN(401) · AuthZ(403) · Validation(400) · **Tenant isolation** · Idempotency(mutating) · Audit-emitted · State-machine(if stateful) · Rate-limit(auth) | Happy + authz redirect |
| **T2 — Standard** | tenant data, moderate blast radius | Happy · AuthZ · Validation · Tenant isolation · Soft-delete/restore(if applicable) | Happy path per page |
| **T3 — Light** | infra / read-mostly | Happy · AuthZ (or mechanism smoke) | Renders, no console errors |

### Scenario taxonomy (precise definitions)

- **Happy** — valid request by an authorized caller returns success + persists.
- **AuthN** — anonymous/invalid-token request returns 401.
- **AuthZ** — *authenticated* caller lacking the required permission returns 403. (Distinct from AuthN. Impersonation tests are the reference implementation.)
- **Validation** — malformed/out-of-range input returns 400 with ProblemDetails.
- **Tenant isolation** — caller in tenant A cannot read/mutate tenant B's data (404/403, never a leak).
- **Idempotency** — replay with same `Idempotency-Key` returns the cached response; different key executes again.
- **Audit-emitted** — the action writes the expected audit-trail row.
- **State-machine** — illegal transitions are rejected; legal ones persist (invoices, tickets, provisioning, impersonation grants).
- **Rate-limit** — auth-sensitive endpoints carry the `auth` policy.

## Coverage matrix (audited 2026-05-23 against ~390 integration tests)

Legend: `✓` covered · `~` thin (harden) · `✗` gap (fill) · `–` N/A

### T1 — Critical

| Feature | Happy | AuthN | AuthZ | Valid | Isolation | Idemp | Audit | State |
|---------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Auth — token issue/refresh/expiry | ✓ | ✓ | – | ✓ | – | – | ~ | – |
| Auth — account lockout | ✓ | ✓ | – | – | – | – | ~ | ~ |
| Auth — 2FA enroll/verify/disable/login | ✓ | ✓ | – | ✓ | – | – | – | ✓ |
| Auth — **forgot/reset password** | ✗ | – | – | ✗ | – | – | – | – |
| Auth — **change password** | ✗ | ✓ | – | ✗ | – | – | – | – |
| Auth — email confirmation | ✓ | – | – | ✓ | – | – | – | – |
| User registration / self-register | ✓ | ✓ | – | ✓ | – | – | – | – |
| User management (CRUD, guards) | ✓ | ✓ | ~ | ✓ | – | – | – | – |
| Roles — CRUD / system protection | ✓ | ✓ | ~ | ✓ | ✗ | – | – | – |
| Roles — permission catalog | ✓ | ✓ | ~ | – | – | – | – | – |
| **Permission-cache invalidation** | ✗ | – | – | – | – | – | – | – |
| Multitenancy — create/activate | ✓ | ✓ | ✗ | ~ | ✓ | – | ~ | ✓ |
| Multitenancy — provisioning lifecycle | ✓ | – | – | – | – | – | – | ~ (fail path ✗) |
| Multitenancy — seed data | ✓ | – | – | – | ✓ | – | – | – |
| Multitenancy — header override | ✓ | – | ✓ | – | ✓ | – | – | – |
| Billing — plans/subscriptions | ✓ | ✓ | ~ | ✓ | ~ (cross-fetch ✗) | – | – | – |
| Billing — invoices | ✓ | ✓ | – | ✓ | ~ (cross-fetch ✗) | ✓ | – | ✓ |
| Billing — usage metering | ✓ | ✓ | – | ✓ | – | ✓ | – | – |
| Impersonation | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Webhooks — subscription CRUD | ✓ | ✓ | ~ | ✗ | ✓ | – | – | – |
| Webhooks — dispatch/retry | ✓ | – | – | ✓ | ✓ | – | – | ✓ |
| Webhooks — **HMAC signature** | ✗ | – | – | – | – | – | – | – |
| Auditing — read/query | ✓ | ✓ | ~ | – | ✓ | – | – | – |

### T2 — Standard

| Feature | Happy | AuthZ | Valid | Isolation | Soft-del |
|---------|:---:|:---:|:---:|:---:|:---:|
| Chat — channels/messages/threads/reactions | ✓ | ✓ | ✓ | ✗ | ✓ |
| Chat — mentions/search/typing/presence/pin/realtime | ✓ | ✓ | ✓ | ✗ | ✓ |
| Chat — file access policy | ✓ | ✓ | ✓ | – | – |
| Files — upload/finalize/validation | ✓ | ✓ | ✓ | ✗ | ✓ |
| Files — **visibility/sharing** | ✗ | ✗ | – | ✗ | – |
| Catalog — products/brands/categories | ✓ | ~ | ✓ | ✗ | ✓ |
| Tickets — lifecycle | ✓ | ~ | ~ | ✗ | ✓ |
| Notifications | ✓ | ✓ | ✓ | ✓ (per-user) | – |
| Groups — CRUD/membership/protection | ✓ | ~ | ✓ | ✗ | – |

### T3 — Light

| Feature | Happy | AuthZ |
|---------|:---:|:---:|
| Health probes (live/ready) | ✓ | ✓ |
| Hangfire dashboard auth | ✓ | ✓ |
| Caching (Redis hybrid) | ✓ | – |
| Idempotency filter mechanism | ✓ | – |

## Prioritized gap backlog (the implementation order for sub-project B)

Each item names the *new* test(s) and explicitly notes what it must NOT duplicate.

### P0 — T1 critical holes (wired-but-unverified or security-load-bearing)

1. **Permission-cache invalidation** — `PermissionCacheInvalidationTests`: after `UpdateRolePermissions` (and `AssignRoles`, group add/remove), a previously-cached `HasPermissionAsync`/`GetPermissionsAsync` reflects the change on the next call. *Not duplicated by* `RolePermissionSyncerTests` (that tests claim restoration, not live cache eviction).
2. **Forgot/Reset password flow** — `PasswordResetTests`: request reset → reset with valid token succeeds + new password logs in; invalid/expired token → 400. *Not covered anywhere today* (only rate-limit wiring references these endpoints).
3. **Change password** — `ChangePasswordTests`: happy path + wrong current password → 400/401. *No existing test.*
4. **Webhook HMAC signature** — extend `WebhookDispatchJobTests`: assert the delivery carries a correct `X-Signature` HMAC over the payload that verifies with the subscription secret. *Harden, don't duplicate* the existing delivery-row test.
5. **Tenant provisioning failure path** — extend `TenantProvisioningStatusTests`: a failing step transitions to `Failed` and `EnsureCanActivate` blocks activation. *Complements* the existing happy-path-only test.

### P1 — T1 cross-tenant & authz hardening

6. **Billing cross-tenant fetch** — extend `BillingEndpointTests`: `GetInvoiceById`/subscription for an id owned by another tenant → 404 (not a leak). *Complements* `GetMyInvoices_Only` (list-leak is covered; direct-fetch is the gap).
7. **Roles tenant scoping** — `RoleTenantIsolationTests`: a role created in tenant A is invisible to tenant B.
8. **403 authz coverage** (thin → covered) — add authenticated-no-permission 403 cases for: Tenant create/manage, Billing plan create, Webhook subscription create, Roles catalog/management, User management, Auditing query. Use the Impersonation 403 tests as the template. *These endpoints currently test 401 only.*

### P2 — T2 isolation & untested features

9. **Chat tenant isolation** — `ChatTenantIsolationTests`: user in tenant A cannot get/join/send-to/search a channel in tenant B (404, no leak). *Not duplicated by* the intra-tenant non-member 404 tests.
10. **Files tenant isolation + sharing** — `FileVisibilityAndSharingTests`: cross-tenant file fetch → 404; a shared file is reachable by the grantee, a private file is not. *The visibility/sharing feature (shipped 2026-05-21) has no test.*
11. **Catalog / Tickets / Groups isolation** — one `*TenantIsolationTests` each proving entities don't cross the tenant boundary.

### P3 — thin-cell hardening (in place, no new files unless noted)

12. Webhook subscription validation (400 on bad URL/event-type).
13. Tickets validation (400 cases on create/transition).
14. Account-lockout window expiry (if feasible without flaky time control; else document as deliberate omission).

## Enforcement design (scopes sub-project C)

The matrix rots unless CI defends it. Two mechanisms:

1. **Trait-driven matrix.** Tag every integration test with xUnit traits: `[Trait("Tier","1")]` and `[Trait("Scenario","TenantIsolation")]` (etc.). A small aggregator (`Tests/CoverageMatrix`) reads the discovered test metadata and asserts: *every T1 feature has a test for each of its required scenario classes.* A missing required cell **fails the build** with a precise message (`T1 feature 'Webhooks' missing scenario 'HmacSignature'`). This makes the matrix executable, not documentation.
2. **Coverage ratchet.** Collect coverage (Coverlet) → upload to Codecov → enforce a threshold that can only rise. Plus the currently-missing frontend CI job (`npm ci && npm run build` + Playwright) and the `Files.Tests`/`Chat.Tests` projects in the matrix.

A feature is registered in a single `feature-registry` (the source of truth the aggregator iterates), so adding a feature without tests is a hard failure — that is what makes the bar *permanent*.

## Testing approach for the new tests

- Reuse `FshWebApplicationFactory` + existing auth/tenant helpers. No new infrastructure.
- Follow the established naming `Method_Should_Behavior_When_Condition` and AAA + `#region` grouping.
- Run against Testcontainers Postgres + MinIO exactly as the current suite does.
- Each new test gets its `[Trait]` tags so it registers in the matrix immediately.

## Open questions

None blocking. Tier assignments above are the working model; adjust during implementation if a feature proves mis-tiered.

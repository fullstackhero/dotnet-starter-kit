# Tenant Lifecycle Automation

Goal: automate tenant provisioning, activation, and health verification so new tenants are production-ready with minimal manual steps while preserving multi-tenant safety, auditing, and observability.

## Scope (In)
- Create/activate tenant triggers background provisioning workflow.
- Per-tenant database creation (or schema), migrations, and seed data.
- Default identity bootstrap (admin user/roles/permissions) tied to tenant.
- Health verification and status reporting.
- Idempotent, retryable orchestration with audit and telemetry.
- Admin endpoints/UX to view workflow state and retry/re-run steps.

## Non-Goals (Out)
- Full-feature feature-flag platform.
- Billing/usage metering.
- Cross-cloud infrastructure automation (K8s, DNS, CDN).

## Personas
- Platform Admin: initiates tenant creation, monitors status, retries failed steps.
- Tenant Admin: receives bootstrap credentials, validates app access post-provision.
- SRE/DevOps: monitors health, investigates failed jobs, tunes resilience.

## High-Level Flow
1) Admin issues `CreateTenant` (or activates an existing tenant).
2) System enqueues a provisioning job (Hangfire) keyed by TenantId + correlation.
3) Workflow steps (all idempotent):
   - Validate tenant metadata (provider, connection string template, validity).
   - Create tenant database/schema (or ensure exists) using provider-specific strategy.
   - Apply EF Core migrations for each enabled module (Multitenancy, Identity, Auditing, etc.).
   - Seed baseline data (roles, permissions, admin user with reset token, root tenant data if applicable).
   - Warm caches if enabled (e.g., permissions).
   - Emit audit + telemetry events for each step.
4) Mark tenant as `Active` when all steps succeed; surface status via API.
5) On failure: capture error, mark status `Failed`, allow retry/resume from failed step.

## Functional Requirements
- Provisioning job:
  - Runs as Hangfire background job; supports manual trigger and automatic trigger on create/activate.
  - Stores per-step status, timestamps, and error messages (persisted per tenant).
  - Uses correlation/trace IDs; logs to OpenTelemetry.
  - Supports cancellation and exponential backoff retries.
- Database orchestration:
  - Provider-aware strategies (PostgreSQL initial target; hooks for SQL Server).
  - Option to create database if missing; else validate connectivity.
  - Runs module migrations in deterministic order; stops on first failure.
- Seeding:
  - Seeds Identity admin user, default roles/permissions, and tenant metadata.
  - Issues one-time admin credential or password reset token for Tenant Admin.
  - Seeds demo data optionally (flag).
- Status surface:
  - API to fetch provisioning status history per tenant.
  - Health check should include tenant provisioning status (ready/degraded/failed).
- Safety & idempotency:
  - All steps re-runnable without corrupting state (check-before-create).
  - Guard against concurrent provisioning for same tenant.
  - Respect tenant validity/activation flags.

## Operational/Observability Requirements
- Emit structured logs with TenantId, correlationId, step name, duration, outcome.
- Create OpenTelemetry spans for each step (db create, migrate, seed, cache warm).
- Publish audit events for lifecycle changes (Requested, Started, StepFailed, Completed).
- Expose metrics: provision_duration_seconds, provision_step_failures_total, active_tenants.

## Security Requirements
- No secrets in logs/audits; hash/scrub credentials.
- Bootstrap credentials delivered via secure channel (email with reset token or out-of-band).
- Enforce tenant isolation during provisioning (context scopes, connection string guards).
- Authorization: only platform admins can trigger or retry provisioning.

## Acceptance Criteria (Happy Path)
- Creating a tenant triggers a job that:
  - Creates/validates DB, applies migrations for all enabled modules, seeds identity/admin, warms caches.
  - Marks tenant Active and Ready; status endpoint shows completed steps with durations.
- Audit trail shows Requested -> Started -> Completed with TenantId and correlationId.
- Metrics and traces include the provisioning spans and surface in health checks.

## Failure/Recovery Criteria
- If migrations fail, status is Failed with error details; job can be retried and resumes idempotently.
- Double-submit provisioning for same tenant does not run concurrent workflows (dedupe/lock).
- Partial seeds are safe to re-run (no duplicate roles/users; admin user upsert).
- Health check reports degraded for tenants with failed provisioning; improves after successful retry.

## Progress Update (Current State)
- Provisioning workflow implemented with persisted status/steps and 202 responses on tenant creation; retry endpoint available.
- Background provisioning via Hangfire, with inline fallback when Hangfire/storage is unavailable (dev-friendly).
- Startup hosted services: tenant catalog migrate/seed (root tenant) and optional auto-provision enqueue.
- Provider-aware TenantDbContextFactory to select PostgreSQL via appsettings.
- Audit pipeline fixed to stamp tenant/user on events; audit sink writes per-tenant batches.

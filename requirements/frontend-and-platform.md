# Frontend & Platform Requirements

Captured: 2026-04-19
Updated: 2026-04-20
Status: Decisions locked — ready for implementation planning.

## Frontend Apps

Two separate deployables. Plain React (Vite) for both. Previous Next.js admin app was removed 2026-04-19.

### Admin App (React + Vite)

Platform-operator-facing application.

- Tenant management
- Quota management
- Billing (internal invoicing)
- (more TBD)

### Dashboard App (React + Vite)

Tenant-user-facing application.

- User management
- Roles
- Permissions
- Product management
- SignalR-driven real-time features (initial scope)
- Recharts-based data visualization
- (more TBD)

## Platform / Cross-Cutting Requirements

### Rate Limiting

- Scope: **per-tenant, per-user, and per-IP** (all three).
- Enforcement: **at the API** (not gateway).

### User Impersonation

- Supports **both directions**:
  - Platform admin → tenant user (support flow).
  - Tenant admin → users within their tenant.
- Audit trail: **tenant admins can view all impersonation audits** for their tenant.

### SignalR

- **Dashboard app only** in the initial rollout. Admin app can follow later.
- Specific features (notifications, live updates, presence, chat) TBD during implementation.

### Charts

- Library: **Recharts**.
- Primary consumer: dashboard app; admin app may adopt later.

### Billing

- **Internal invoicing** only in initial scope. No third-party provider integration (Stripe/Paddle/Chargebee).

### Quota Management

- Meter **all of**: API calls, storage, users, and feature flags.
- Enforcement point: TBD during design (likely API middleware + per-feature checks).

### Object Storage

- **MinIO** in `docker-compose.yml` for local dev (S3-compatible).

## Decisions Summary

| # | Question | Decision |
|---|----------|----------|
| 1 | One app or two? | **Two** — separate deployables |
| 2 | Rate limiting scope / enforcement | **Per-tenant + per-user + per-IP**, enforced **at API** |
| 3 | Impersonation direction / audit | **Both directions**; tenant admins see all audits |
| 4 | Charts library | **Recharts** |
| 5 | SignalR scope | **Dashboard app** initially |
| 6 | Billing | **Internal invoicing** only |
| 7 | Quota metering | **All** — API calls, storage, users, feature flags |

# Admin → Dashboard Design Unification

- **Date:** 2026-05-28
- **Status:** Draft (awaiting review)
- **Author:** Claude (with Mukesh)
- **Approach:** A — copy/mirror the dashboard's design system into the admin app
- **Tracking branch:** `feat/admin-dashboard-unification`

## 1. Summary

Reskin the operator app (`clients/admin`) so it adopts the tenant app's
(`clients/dashboard`) design system **wholesale** — tokens, component library,
layout chrome, and page structures — until the two apps are visually
indistinguishable in look-and-feel (differing only in their feature sets and
navigation targets).

This is a **multi-phase program**, not a single change. Each phase is built,
linted, type-checked, and visually eyeballed before the next begins.

## 2. Context

The repo ships two React 19 + Vite 7 SPAs that already share an architecture
(TanStack Query v5, React Router 7, Radix + Tailwind v4 shadcn-style, a
hand-written `apiFetch`, runtime `/config.json`). They deliberately diverged in
*visual language*:

- **admin** — "Console": an editorial-terminal aesthetic. `// CONSOLE`
  masthead, `\ SECTION` mono-caps markers, sharp dark-first chrome, a
  chartreuse-lime (`--signal-*`) accent, a grid-texture canvas, and a
  documentation-aside form layout (`FormShell`/`FormSection` with a fixed 18rem
  left rail).
- **dashboard** — a warmer, calmer editorial card system: header-bar section
  cards (`SettingsSection`), a warm brand/saffron accent, softer surfaces, an
  editorial numbered settings nav, a command palette, and space-efficient
  `sm:grid-cols-2` field layouts.

**Decision (owner):** unify on the **dashboard's** language. The admin app's
distinct console identity (masthead, `\` markers, chartreuse signal, grid
texture) is intentionally retired in favor of one consistent system.

### Why this is worth doing
- The dashboard layout is materially more space-efficient — the admin's 18rem
  description rail and width-capped single-column forms waste horizontal space
  on every editor page.
- The dashboard's component set is richer and more polished (dropdown-menu,
  avatar, switch, command palette, presigned `ImageInput`, a complete toast
  theme).
- One language across both apps lowers cognitive load for operators who use
  both and reduces design drift.

## 3. End state ("done" looks like)

- `clients/admin/src/styles/globals.css` is a copy of the dashboard's token
  system (palette, surfaces, radii, shadows, motion, fonts, base, utilities),
  adapted only where admin genuinely needs an extra token.
- `clients/admin/src/components/ui/*` matches the dashboard's component set and
  styling (plus the dashboard-only components admin now needs: `avatar`,
  `dropdown-menu`, `switch`).
- The admin app shell (sidebar + topbar) reads as the dashboard's shell, not
  the `// CONSOLE` masthead.
- Every admin page uses the dashboard's section-card / field-grid layout
  vocabulary. No page still uses `FormShell`/`FormSection`'s 18rem rail.
- Avatar upload uses the dashboard's presigned `ImageInput` (fixes the current
  data-URL-vs-2048-cap bug as a by-product).
- `npm run build` (tsc + vite) and `eslint` pass for `clients/admin`; the
  Playwright suite still passes (selectors updated where markup changed).

## 4. Approach

**Chosen: A — copy/mirror.** Port the dashboard's tokens/components/pages into
admin file-by-file. Two independent apps that happen to look identical. Simplest
mental model, no monorepo/workspace tooling, each app still builds and deploys
on its own.

**Rejected for now:**
- **B — shared design-system package.** Single source of truth, zero drift, but
  a large upfront refactor (workspace tooling, move components, rewire imports
  in *both* apps) before any visible result.
- **C — hybrid (copy now, extract later).** Reasonable, but the owner chose a
  clean copy/mirror; extraction can be reconsidered if maintaining two copies
  becomes painful (see Open Questions).

**Consequence of A:** any future design change must be applied to both apps.
Accepted.

## 5. Non-goals

- No backend/API changes (except none — purely `clients/admin`).
- No change to admin's **routes, permissions, RouteGuards, or data layer**
  (`src/api/*`, query keys). This is a visual/structural reskin only.
- Not touching `clients/dashboard` (it is the reference, not a target).
- Not introducing new features to admin; pages keep their current capabilities.
- Not adopting dashboard-only *features* that admin has no backend for (e.g.
  command palette is optional and deferred — see Phase 3).

## 6. Inventory

### 6.1 Token layer
Both apps expose the same semantic layer via `@theme inline`
(`--color-foreground`, `--color-card`, `--color-border`, `--color-primary`,
`--color-muted-foreground`, `--surface-1..4`, `--color-success/warning/info/
destructive`, `--ease-out-cubic`, `--font-display/mono`, …). This shared
surface is what makes copy/mirror safe — components keep referencing the same
names; only the *values* change.

| | admin (current) | dashboard (target) |
|---|---|---|
| Accent scale | `--signal-*` (chartreuse) | `--brand-*` + `--color-saffron` (warm) |
| Neutrals | `--neutral-*` hue 240 | dashboard neutrals (untinted; see memory) |
| Extras | `--accent-signal*`, `--grid-alpha` | `--color-overlay`, `--color-primary-hover`, `--chart-*`, `--font-feature-*` |

**Reconciliation:** copy the dashboard's `:root`/`.dark`/`@theme inline`
blocks verbatim, then add the *handful* of admin-only tokens still referenced by
not-yet-migrated admin code so nothing breaks mid-migration; remove them as the
referencing components migrate.

### 6.2 Components

| Group | admin has | dashboard has | action |
|---|---|---|---|
| `ui/` | badge, button, card, dialog, input, label, skeleton, table | + avatar, dropdown-menu, switch (no table) | port dashboard versions; **keep** admin's `table` (dashboard has none), restyled to tokens |
| layout | app-shell, sidebar, topbar, mobile-nav, nav-items, sidebar-content | layout/* (dashboard shell) | rebuild admin shell on the dashboard pattern |
| list | `FormShell`/`FormSection`/`FormActions`, `PageHeader`, `SectionRule` | `SettingsSection`, `Field`, `EntityPageHeader`, list primitives | replace admin form primitives with dashboard equivalents |
| file | — | `ImageInput` + `use-file-upload` + `api/files` | port for avatar + any image fields |

### 6.3 Pages (admin → dashboard analog)
audits (list/detail) · auth (confirm-email/forgot-password/reset-password) ·
billing (invoices/plans/layout) · dashboard(overview) · health · impersonation ·
login · not-found · notifications/inbox · roles (list/create/detail) · settings
(layout/profile/security/sessions/appearance) · tenants (list/create/detail) ·
users (list/create/detail) · webhooks (list/detail).

The dashboard has close analogs for most (identity↔roles/users, settings/*,
health, audits, login, auth/*, overview↔dashboard). Admin-only pages (tenants,
impersonation, billing-plan authoring, webhooks) get the *vocabulary* applied
without a 1:1 source page.

## 7. The program (phases)

> Ordering is bottom-up: foundation first so each later layer renders correctly
> against the new base. Every phase ends green (build + lint + type-check) and
> is committed before the next starts.

### Phase 1 — Tokens & global styles
Port the dashboard's `globals.css` into admin (palette, surfaces, radii,
shadows, motion, fonts, base layer, shared utilities, sonner block). Reconcile
token names (§6.1). Drop admin's grid-texture canvas + `// CONSOLE` chrome
styling. **Exit:** admin builds; pages render with the new palette (some still
structurally old — acceptable mid-program).

### Phase 2 — Shared UI primitives + form layout
Port/align `ui/*` (button, input, badge, card, dialog, label, skeleton; add
avatar, dropdown-menu, switch; restyle `table`). Replace `FormShell`/
`FormSection`/`FormActions` usages with `SettingsSection` + `Field`. Port the
`list/` primitives and `EntityPageHeader`. **Exit:** primitives match the
dashboard; a sample page (Settings/Profile) fully converted as the reference.

### Phase 3 — App shell
Rebuild `components/layout/*` (sidebar, topbar, mobile-nav) on the dashboard's
shell. Retire the `// CONSOLE` masthead and `platform · administration ·
interface` footer. Command palette: **deferred/optional** (port only if low
effort; not required for "done"). **Exit:** navigating admin feels like the
dashboard shell.

### Phase 4 — Pages (sub-phased, each its own commit)
Apply the section-card/field-grid vocabulary page-group by page-group, in
rising blast-radius order:
1. **settings/** (profile + **presigned avatar via `ImageInput`**, security,
   sessions, appearance, layout)
2. **roles/** + **users/** (list/create/detail)
3. **tenants/** (list/create/detail — reuses the already-fixed provisioning UI)
4. **billing/**, **webhooks/**, **audits/**, **notifications/**, **health/**,
   **impersonation/**
5. **auth/** + **login** + **not-found** + **dashboard(overview)**

**Exit:** no page references retired primitives; Playwright selectors updated.

## 8. Verification (every phase)
- `cd clients/admin && npm run build` (tsc -b + vite build) — must pass
  (`TreatWarningsAsErrors` analog: tsc is strict).
- `npx eslint .` — clean.
- `npx playwright test` for the touched suites — green (update route-mocked
  selectors where markup changed; mocks themselves shouldn't need changes since
  the data layer is untouched).
- Manual: run the admin dev server, eyeball the migrated pages in light + dark.

## 9. Risks & mitigations
- **Mid-migration breakage** (a component restyled before its consumers) →
  keep admin-only tokens alive until their consumers migrate (§6.1); phases are
  bottom-up so consumers migrate after primitives.
- **Playwright drift** — markup/class changes break selectors → update
  selectors per page-group in the same commit; data mocks stay valid (data
  layer untouched).
- **Scope creep into features** — strict non-goals (§5); reskin only.
- **Unrelated in-flight work** — the working tree currently carries someone
  else's uncommitted chat/realtime changes; this program touches only
  `clients/admin/**` (+ this spec) and never stages those files.
- **Identity loss is intentional** — the console aesthetic is being retired by
  explicit owner decision; not a regression.

## 10. Rollback
Each phase is an isolated commit on `feat/admin-dashboard-unification`. Any
phase can be reverted independently. The whole program lives behind one PR; if
abandoned, the branch is dropped with zero impact on `main` or the dashboard.

## 11. Open questions
- **Shared package later?** If drift between the two copies becomes painful,
  revisit Approach B (extract `packages/ui`). Out of scope for this program.
- **Command palette in admin?** Deferred. Decide during Phase 3 based on effort.
- **`table` primitive** — admin has one the dashboard lacks; keep + restyle, or
  replace admin's table-using pages with the dashboard's list-row pattern?
  Resolve when Phase 4 reaches the first table page.

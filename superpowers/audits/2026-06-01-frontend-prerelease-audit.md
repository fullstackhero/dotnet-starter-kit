# Frontend pre-release audit — admin + dashboard

Date: 2026-06-01 · Scope: `clients/admin` (operator) and `clients/dashboard` (tenant), React 19 + Vite 7 + TS + Tailwind v4 + Radix/shadcn.
Goal: production-grade, excellent UX, no inline/duplicated code (extract to shared components), and a **uniform look and feel** across both apps.

Baseline at audit time: both apps build clean (`tsc -b && vite build`). The work below is incremental hardening, not a rescue.

## Headline finding

The **design foundation is already uniform**: `clients/admin/src/styles/globals.css` and `clients/dashboard/src/styles/globals.css` are **byte-identical** — same OKLCH primitives, untinted neutrals (chroma 0), rose brand, 4-tier surfaces, Outfit/Figtree/JetBrains-Mono type stack, radii, motion, shadows. So uniformity is a matter of closing a handful of *component-level* and *appearance-feature* gaps, not a redesign.

## Uniformity gaps (admin ⟷ dashboard)

| Dimension | admin | dashboard | Action |
|---|---|---|---|
| Design tokens (`globals.css`) | identical | identical | none ✅ |
| `Button` variants | extra `signal` | canonical set | remove `signal`, map call-sites → `default` |
| `Badge` variants | extra `muted` | canonical set | remove `muted`, map call-sites → `default` |
| `Select`, `Table`, `ConfirmDialog` primitives | present | **missing** | mirror into dashboard `components/ui/` |
| Shared list family | `EmptyState`/`Field`/`LoadingRow`/`ErrorBand` | richer `Entity*` family (`EntityPageHeader`, `EntityPager`, `EntityListCard/Header/Row`, `EntityMobileCard`, `EntityStatusBadge`, `EntityDetail*`) | converge on one vocabulary (dashboard's `Entity*` is the more complete reference) |
| Appearance system | light/dark toggle only | mode(system)+accent(6 presets+custom)+font(12)+density+reduced-motion, View-Transitions, index.html bootstrap | **port dashboard's appearance system to admin** (biggest UX/uniformity gap) |
| Storage keys | `fsh.admin.*` | `fsh.*` | leave as-is (renaming drops users' persisted prefs; low value) |
| Forms | React Hook Form + Zod | ad-hoc per page (no RHF/Zod, no textarea/select/checkbox primitives) | adopt RHF+Zod + form primitives in dashboard |

## DRY / inline-code hotspots

**Dashboard** (has the shared components but doesn't use them everywhere):
- `pages/catalog/products.tsx`, `brands.tsx`, `categories.tsx` hand-roll pagination + table header/rows instead of `EntityPager` + `EntityListCard/EntityListHeader/EntityListRow` (which `tickets.tsx`/`users.tsx` already use).
- Duplicated ticket enum maps (`STATUS_LABEL/TONE`, `PRIORITY_LABEL/TONE`) in `pages/tickets/tickets.tsx` **and** `ticket-detail.tsx` → extract to a shared module.
- Repeated `onError: toast.error(...)` mutation boilerplate → `useMutationToast()` hook.
- Repeated search-debounce `useEffect` → `useDebounce()` hook.
- No `FormDialog` shell wrapper (every editor dialog repeats `DialogHeader/Body/Footer` + Cancel/Submit).

**Admin** (needs the shared components extracted):
- Hand-rolled status badges in 6+ places (`pages/users/list.tsx`, `roles/list.tsx`, `tenants/list.tsx`) → `StatusBadge`.
- Duplicated `MobileListCard` / `DesktopListRow` grid+hover patterns across users/roles/tenants → extract.
- Inconsistent empty states (sometimes `EmptyState`, sometimes inline) → standardize.

## UX / production gaps

- **Detail-page skeletons inconsistent**: dashboard product/ticket detail skeleton-load; `user-detail`, `group-detail`, `role-detail` (dashboard) and admin `users/detail` go blank or show raw "Loading…" text.
- **Missing empty states**: dashboard `system/audits.tsx`, `system/activity.tsx`, `system/sessions.tsx`.
- **Mutation buttons lack pending state** in several dashboard editor dialogs (double-submit risk).
- **No optimistic updates** anywhere (lists wait for refetch after delete/edit — feels laggy).
- **Row-level permission checks** missing in admin (e.g. webhook delete button shows for users who'll get 403).
- **Error recovery**: `ErrorBand` has no inline "Retry"; admin `app-shell` Suspense fallback uses raw "Loading…".
- **Focus**: dialogs/mobile drawers should trap focus and restore to trigger on close (verify Radix coverage).
- **Truncation**: long names can overflow tight desktop grid cells (add `truncate`/`line-clamp`).

## Per-app maturity (from page inventory)

- **Dashboard**: chat (advanced), files (advanced), catalog/tickets (feature-complete but duplicative), identity/system/settings (functional→basic; several placeholders: `system/overview`, `settings/notifications`, `api-keys`, billing pages).
- **Admin**: dashboard/users/roles/tenants/webhooks/audits/billing-invoices (complete); settings security/sessions, auth recovery, health (basic/minimal).

## Prioritized roadmap

**Batch 1 — Dashboard DRY (low risk, uses existing proven components):** convert products/brands/categories to `EntityPager` + `EntityListCard/Header/Row` + `EntityEmpty`; extract `lib/ticket-enums.ts`; add `EntityEmpty` to audits/activity/sessions. *(implemented in the accompanying PR)*

**Batch 2 — Primitive uniformity (low risk):** reconcile admin `Button.signal`/`Badge.muted` to the canonical set; mirror `Select`/`Table`/`ConfirmDialog` into dashboard. *(partially in PR — see commits)*

**Batch 3 — Appearance parity (medium, high UX value):** port dashboard's appearance system (accent/density/font/system-mode + index.html bootstrap + View Transitions) to admin so both apps offer identical theming. Largest single uniformity win; staged for review because it rewrites admin's `ThemeProvider`.

**Batch 4 — Forms (medium):** add shared `Textarea`/`Select`/`Checkbox`/`FormField` primitives + adopt RHF+Zod in dashboard; add a `FormDialog` shell to both.

**Batch 5 — UX polish (low/medium):** detail-page skeletons everywhere, optimistic list updates, mutation pending states, `ErrorBand` retry, row-level permission gating in admin, truncation.

Batches 3–5 carry UX-bearing decisions and broad surface area; they are staged rather than landed unsupervised so the changes can be reviewed against the live apps.

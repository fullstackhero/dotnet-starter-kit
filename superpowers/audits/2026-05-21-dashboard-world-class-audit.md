# Dashboard — World-Class Readiness Audit

**Scope:** `clients/dashboard` (tenant-side React app).
**Date:** 2026-05-21.
**Method:** six parallel read-only audit agents (style, accessibility, dead code, performance, UX, packages). All findings cross-verified with `file:line` evidence.
**Stack baseline:** React 19, Vite 7, TypeScript 5.9, Tailwind v4, Radix, TanStack Query 5, React Router 7, SignalR 10, sonner, cmdk, lucide.

> **Status (2026-05-21 evening rollup):** P0 cleared in entirety, most P1 landed.
> See [[dashboard_a_plus_followups]] for the deferred items (server-touching
> and sprint-scale architecture). Build + lint + Playwright 37/37 green.
> Main JS chunk dropped 197.5 KB → 178.0 KB gzip (−10%). Two new lazy
> chunks: `command-palette-dialog` (8.2 KB gzip) and SignalR (14.5 KB gzip).
> Chat-specific CSS extracted to a 3.9 KB per-route chunk. Eager Google
> Fonts trimmed from 12 families to 3. Six unused npm deps + 10 orphan/dead
> files removed. `recharts` dropped (declared, never imported).

---

## TL;DR — what shipped vs what's still in the way

**What's already world-class.** Token architecture is textbook three-layer oklch with the untinted-neutrals fix held (zero `text-gray-*`/`bg-zinc-*` leaks anywhere). View-Transitions theme crossfade via `flushSync` is genuinely advanced. Cross-app version drift is **zero**. JWT refresh has stampede protection. Every route is lazy-loaded (31/31). nginx cache strategy is canonical (1y immutable on hashed assets + no-store on `index.html`/`config.json`). No TODO/FIXME/HACK/debugger anywhere in `src/`. ESLint `jsx-a11y/recommended` is enforced.

**The four things blocking "best-in-class 2026":**

1. **Command palette is navigation-only.** It carries Overview/Activity/Invoices/Health/Audits/Settings — but not Identity, Catalog, Tickets, Files, or Chat. No "Create…" group. No record search. This is the single biggest perceived-completeness gap vs Linear/Stripe/Vercel.
2. **No URL state for filters/sort/page on any list.** Every page (Audits, Users, Products, Tickets, Sessions, Trash) keeps state in `useState`. Bookmarks, share-with-a-teammate, browser back/forward — all broken. Stripe/Linear/Vercel/Resend all do this.
3. **No bulk actions anywhere.** No checkbox column, no sticky multi-select bar. Sessions admin can only revoke one or "all". Trash has no bulk-restore. Tickets/Users/Products all force per-row mutations. Table-stakes for any admin tool.
4. **Form errors silent for screen readers, half the chrome below WCAG 2.2 AA.** Zero `aria-invalid`/`aria-describedby` usage. No `aria-current="page"` on NavLinks. Muted text in dark mode at `oklch(0.680 0 0)` already borderline, then alpha-multiplied to ~2.8:1 for placeholders/chevrons — **fails AA**. No `<table>` semantics anywhere — every list is a `<div>` grid. Touch targets `h-5 w-5` (20×20) on multiple icon-only controls fail WCAG 2.2 SC 2.5.8.

**Three quick visual fixes that change the room temperature:**

- Define a 4-step type scale; today h1 sizes drift across `EntityPageHeader` (22px), `PageHero` (26px), `OverviewPage` (28px), `ListHero` (32px), `NotFoundPage` (32px). Same role, four sizes.
- Drop `backdrop-grayscale` from the Dialog scrim — the page going greyscale behind modals reads as a system error, not focal contrast.
- Light-mode elevation is flat: `--surface-3`, `--card`, `--popover` all equal `--neutral-0`. Dark mode is correctly layered. Re-introduce a one-step difference so cards float again.

**One signature move worth committing to:** the rose→saffron 1px gradient strip at the top of `EntityDetailHero` is the only place the brand actually paints on a page surface. Promote it to **every** page header (list + detail + settings + chat channel header) and to the page-count chip color. Two repeated identity moves are enough.

---

## Composite scorecard

| Dimension | Grade | One-sentence verdict |
|---|---|---|
| Tokens / design system | A− | Architecture and discipline are excellent; only drift is shadow literals and an undefined sheet keyframe set. |
| Typography | C+ | Tokens are defined but per-page h1 sizes diverge; 12 fonts loaded eagerly on every page. |
| Color & elevation | B− | Dark mode layered, light mode flat. Muted-token alpha multiplication breaks contrast. |
| Motion | B | Reduced-motion honored globally; `animate-fsh-sheet-*` keyframes are missing. |
| Accessibility (WCAG 2.2 AA) | C | ~62–68% estimated AA conformance. Five named blockers below. |
| Dead code | A | Only 4 orphan files, 6 unused deps, 1 console.warn. No TODOs at all. |
| Performance | B+ | 411 KB JS gzip total (197 KB main shell). Lazy routes everywhere. Big wins available in fonts + SignalR + SSE context. |
| UX patterns vs Linear/Vercel/Stripe | C+ | Strong primitives, but command palette, URL state, bulk actions, optimistic UI, inline editing all materially behind. |
| Packages / security | A− | 1 transitive moderate advisory, auto-fixable. Zero cross-app drift. |

**Overall: B/B+** — a polished foundation that's punching below its potential because the high-leverage UX patterns (URL state, palette, bulk actions, undo) haven't been built.

---

## The Punchlist

Ordered by **value-per-hour**, not by category. Effort estimates assume Mukesh + Claude pair work.

### P0 — Ship in the next two weeks (highest visible/ROI per hour)

| # | Title | Effort | Source | Why it's P0 |
|---|---|---|---|---|
| 1 | **Drop `backdrop-grayscale` from Dialog scrim** (`dialog.tsx:36`) | 5 min | Style | Removes "the page got sick" effect under every modal. |
| 2 | **Define a 4-step type scale** `--text-display-page/section/card/stat` and migrate `EntityPageHeader`, `PageHero`, `OverviewPage`, `NotFoundPage` h1s | 1 h | Style | Biggest single visual cohesion win. |
| 3 | **Lazy-load `@microsoft/signalr`** via dynamic import in `realtime/realtime-context.tsx:11-17`; move `RealtimeProvider` + `SseProvider` out of `app-shell.tsx:17-18` to only the routes that need them | 2 h | Perf | Saves 37 KB gzip (~19% TTI) on the main shell + stops opening a SignalR hub on every authenticated route. |
| 4 | **Cull eager Google Fonts to 3 (Figtree/Outfit/JetBrains Mono)** in `index.html:14-17`; lazy-inject the other 9 when Appearance settings opens | 1 h | Style + Perf | 200–400 KB saved on cold load. |
| 5 | **Add every leaf nav route + a Create group to command palette** (`command-palette.tsx:142-228`) | 2 h | UX | Identity, Catalog, Tickets, Files, Chat aren't navigable today. Creates Linear/Stripe-class perceived completeness. |
| 6 | **Add `aria-current="page"` to every NavLink** in sidebar, mobile-nav, settings-layout | 30 min | A11y | One-line fix per NavLink; SR users have no active-state signal today. |
| 7 | **Wire `aria-invalid` + `aria-describedby` on login/forgot-password/reset-password forms**; introduce `FormField` primitive in pass two | 2 h (this pass) | A11y | Zero aria-invalid usage app-wide. SR users get no error link to a field. |
| 8 | **Run `npm audit fix` + Wave-1 package bumps** (react 19.2.6, @tanstack/react-query 5.100.11, react-router 7.15.1, tailwindcss 4.3.0, etc.) | 1 h | Packages | Clears the only advisory; aligns everything to current. One PR, one smoke. |
| 9 | **Remove dead deps from dashboard:** `zod`, `@hookform/resolvers`, `@types/lodash`, `autoprefixer`, `recharts`, `react-hook-form` (recharts is declared but zero imports) | 30 min | Packages + Dead-code | Trims `node_modules` ~120 KB; eliminates re-introduction footguns. |
| 10 | **Delete orphan files**: `components/file/file-gallery.tsx`, `components/sse/live-feed.tsx`, `components/sse/sse-status-badge.tsx`, `components/theme/theme-toggle.tsx`; remove empty `components/sse/` | 20 min | Dead-code | Zero blast radius; verified no consumers. |
| 11 | **Define `--text-*` and `transitionDuration: { fast, default, slow }` Tailwind utilities** in the `@theme inline` block at `globals.css:357-409` so `duration-fast` etc. become real classes | 30 min | Style | Eliminates `duration-150 / 200 / 100` drift across 38 files. |
| 12 | **Split `SseContext` into status vs events** (`sse/sse-context.tsx:186-189`) so `OverviewPage` doesn't re-render its 1156-line tree on every SSE event | 1 h | Perf | Eliminates the worst re-render hotspot. |
| 13 | **Bump icon-only buttons under 24×24 to 24+** (`h-5 w-5` in `composer.tsx:524-536/609-621/677-688`, `channel-rail.tsx:131/228-237`, settings nav) | 1 h | A11y | WCAG 2.2 SC 2.5.8 AA new criterion. |
| 14 | **Mount RouteError detail behind `import.meta.env.DEV`** (`route-error.tsx:25-29`); show correlation id + "Report" in prod | 30 min | UX + Security | Stops dumping `error.stack` to end users. |
| 15 | **Set `aria-expanded={!collapsed}` on sidebar collapse button** (`sidebar.tsx:115-117`); add `aria-label="Search commands"` to cmdk input; add `aria-label={`Message ${channelTitle}`}` to chat composer textarea | 20 min | A11y | Trio of quick programmatic-label fixes. |
| 16 | **Conditionally render `DialogDescription`** (don't pass whitespace) in `file-preview-dialog.tsx:149-156` | 5 min | A11y | SRs announce the whitespace as the dialog description. |
| 17 | **Use `useUserDisplay` on tickets list + detail** (`tickets.tsx:399-413`, `ticket-detail.tsx:223-329`) instead of `.slice(0, 8) + "…"` user-id rendering | 40 min | UX | Hook exists; tickets surface looks unfinished without it. |
| 18 | **Bring back light-mode elevation** in `globals.css:123-128`: `--surface-3 ≠ --card ≠ --background` (e.g. `--surface-3 = oklch(0.992 0 0)`) | 20 min | Style | Cards regain "sheet on desk"; today they're hairline-flat. |

### P1 — Sprint 1 (next 2–4 weeks)

| # | Title | Effort |
|---|---|---|
| 19 | **URL-sync filters/search/page/sort on Audits as the template**, then roll to Users, Tickets, Products, Brands, Categories, Files. Implement `useUrlState<T>` hook. | 1 d for template + 2 h per page |
| 20 | **`EntityBulkBar` primitive + `useRowSelection<T>` hook + checkbox column**, deploy to Users, Sessions, Trash, Products, Tickets, Audits, Files | 1 d primitive + 0.5 d per page |
| 21 | **`?` keyboard shortcut overlay + `tinykeys`-backed global hotkeys**: `c` to create, `/` to search, `j/k` row nav, `gi/gp/gs/gc/go/gt` route prefixes | 1 d |
| 22 | **Action-with-undo toast pattern** in `App.tsx` + sample callers (deleteRole/deleteUser/deleteProduct/deleteChannel/deleteMessage) | 0.5 d |
| 23 | **Migrate list surfaces to `<table>` semantics** (or at minimum `role="table"/row/columnheader/cell` in `entity-shell.tsx:267-333`); Users/Roles/Groups/Audits/Sessions/Trash/Invoices/Tickets/Products/Brands/Categories | 1–2 d |
| 24 | **`role="log" aria-live="polite"` on `MessageList` + Activity feed; `role="status" aria-live="polite"` on typing indicator** | 0.5 d |
| 25 | **ARIA 1.2 combobox pattern** on mention picker (`pages/chat/composer.tsx:380-420` + `mention-picker.tsx:32-39`), `UserPicker`, and `Combobox` primitive — `role="combobox"`, `aria-controls`, `aria-expanded`, `aria-activedescendant` | 1 d per widget = ~3 d |
| 26 | **Reactions popover + composer-attachment chip + reply-quote insertion** focus-management pass: close on Esc + outside-click, return focus to trigger, move focus to composer when reply is added | 0.5 d |
| 27 | **Focus-visible single source of truth**: keep the global `:focus-visible` outline at `globals.css:536-539` OR strip per-component `focus-visible:ring-[3px]` from Button/Input/Combobox — not both | 0.5 d |
| 28 | **Cull dead list primitives**: delete `density-toggle.tsx`, `empty-state.tsx`, `list-hero.tsx`, `pagination.tsx`, `sort-chips.tsx`, `stat.tsx`; prune barrel re-exports in `components/list/index.ts` | 30 min |
| 29 | **Wire functional density toggle into `pages/settings/appearance.tsx`** (provider + tokens exist; UI missing) | 1 h |
| 30 | **Field-error model: `FormField` primitive owning id + `aria-invalid` + `aria-describedby` + error-region rendering**; migrate login/forgot/reset + 4 main dialogs | 2 d |
| 31 | **Virtualize**: `audits.tsx`, `sessions.tsx`, notifications inbox dropdown, `my-files.tsx`, `activity.tsx`/`live-feed.tsx` (already a dep — `@tanstack/react-virtual`) | 0.5 d each |
| 32 | **`React.memo` on list row components**: `NotificationRow`, `RecentAuditRow`, `FeedRow`, `DesktopRow`/`MobileCard`, chat `Message` (after Sse split lands) | 0.5 d |
| 33 | **`PresenceDot` primitive** (size + userId; calls `usePresence`) — apply to user-detail, sessions, tickets list, audits user column | 0.5 d |
| 34 | **`isDirty` nav guard via `useBlocker`** on role-detail and user-detail editors | 0.5 d |
| 35 | **`Pagination` consolidation**: pick one primitive (recommend `EntityPager`); delete the other; replace catalog/products' hand-rolled pager | 1 h |
| 36 | **Show 2FA recovery codes after enroll** (`security.tsx:482-503`); server returns codes, UI presents copy + download | 0.5 d (after server lands) |
| 37 | **Resend email confirmation action** on user-detail page (API exists) | 30 min |
| 38 | **`MarkdownComposer`** extracted from chat composer for re-use in ticket comments (`ticket-detail.tsx:452-468`) | 1 d |
| 39 | **Promote brand gradient rule into `EntityPageHeader`, `PageHero`, settings layout header, chat channel header** | 1 h |
| 40 | **Wave-3 majors, one at a time:** `@vitejs/plugin-react` 6, `eslint-plugin-react-hooks` 7, `lucide-react` 1, `recharts` 3 (only after we decide whether to keep it), `@hookform/resolvers` 5 (admin only), `zod` 4 (admin only) | 1 PR/major ~ 5 d total |

### P2 — Sprint 2+

| # | Title |
|---|---|
| 41 | **`BroadcastChannel`-shared SignalR connection across tabs** — leader election, cut N tabs → 1 server connection. |
| 42 | **CSP + HSTS + Permissions-Policy headers** in `docker/nginx.conf`. |
| 43 | **Self-host Geist + JetBrains Mono** as woff2 with `preload`; deprecate Google Fonts CDN entirely. |
| 44 | **Inline editing primitive** + roll to product price/name, brand/category name, role description, ticket title, channel description. |
| 45 | **Optimistic mutations** as the default; convert the 30+ pessimistic `useMutation` calls. |
| 46 | **Saved views per list** (localStorage v1, per-tenant v2). |
| 47 | **Lightbox** for image attachments in chat + files. |
| 48 | **Bottom-tab mobile nav** below 768px, pull-to-refresh, swipe row actions. |
| 49 | **Empty states with sample-data CTA** (Linear/Vercel pattern). |
| 50 | **`select` query narrowing** in chat MessageList + audits to stop sibling cache writes triggering re-renders. |
| 51 | **Move `tailwind-merge` out of hot `cn()` paths**; pre-bake variants via `class-variance-authority` for Button/Avatar/Badge/list rows. 16 KB gzip in main shell today. |
| 52 | **Move chat-specific CSS** (lines 1091-1326 of `globals.css`) into `pages/chat/chat.css` for per-route CSS chunk. |
| 53 | **CSV / SCIM bulk user invite** dialog. |
| 54 | **Folder organization for Files** + per-user/per-channel share + expiring share-links. |
| 55 | **Per-resource role presets** in role-detail (Read-only billing, etc.). |
| 56 | **i18n scaffolding** (react-intl or i18next), then RTL-readiness via Tailwind logical properties. |
| 57 | **Workspace consolidation** of `clients/admin` + `clients/dashboard` to pnpm workspaces. |
| 58 | **`@axe-core/playwright` sweep** in CI on top routes. |
| 59 | **`knip` as a CI gate** at `clients/dashboard` and `clients/admin`. |
| 60 | **Renovate config** at repo root (template included below). |

---

## Per-dimension highlights

Each subsection is a compressed reference to the full agent finding. Cite line numbers verbatim — they're the source of truth.

### Visual style + design system

**Strengths:** token discipline pristine; zero hard-coded palette leakage; untinted-chassis fix held; brand-mark + rose primary + saffron accent + chart-1..5 form a coherent palette; View-Transitions theme crossfade is genuinely advanced; toast styling override is the most distinctive piece of chrome.

**Critical findings:**

- **No coherent type scale.** Same-role h1s render at 22 / 26 / 28 / 32 / 32 px across pages. Define `--text-display-page/section/hero/stat`; migrate every primitive.
- **`fsh-sheet-in-left/right/top/bottom` keyframes are referenced but undefined** in `dialog.tsx:143-146`. Mobile drawer snaps open without animation. Add to `globals.css` or rebind to existing `fsh-dialog-in/out`.
- **`recharts ^2.15.0` declared in `package.json:34` but zero `src/` imports.** Pure dead weight. Either wire (Usage row in `overview.tsx:1009-1064` is a candidate) or remove.
- **`shadow-[0_1px_2px_oklch(0_0_0_/_0.04)]` literal repeated 7+ times** (`tickets.tsx:315`, `ticket-detail.tsx:699/831/860`, `entity-shell.tsx:91/253/348/484`, `entity-detail.tsx:66/291`) instead of `shadow-xs` (which uses 0.05 alpha — the literal doesn't even match). Sed-able fix.
- **Hover-border trick `oklch(from var(--color-border) l c h / 1.4)`** in `overview.tsx:175/638/836` clips alpha to 1.0 silently — no visible hover effect. Use `var(--color-border-strong)`.
- **DropdownMenuLabel composition produces two visual sizes** because `dropdown-menu.tsx:99` AND `topbar.tsx:297` both apply mono-uppercase-tracked classes. Lock label rendering inside the primitive.
- **Skeleton shimmer (2.4s sweep) feels anxious** with 10+ rows; stagger animation-delay per child.
- **5 of 13 list/ primitives are dead**: `ListHero`, `Stat`/`StatStrip`, `SortChips`, `DensityToggle`, `Pagination`, `EmptyState` have zero page consumers. The framework looks bigger than it is.
- **`--font-display` doesn't switch with the Font picker** (`theme-provider.tsx:137-141` sets only `--font-sans`). Either pin Outfit as display or make it follow body.
- **Charts are tokenized** (`--chart-1..5`) but no chart library uses them.
- **Default body font is Figtree**, not the "Geist + JetBrains Mono" the brief implied. Pick one and declare it.

### Accessibility (WCAG 2.2 AA)

**Estimated current conformance: ~62–68%.** Strong bones, blockers in the data-density surfaces.

**Top blockers (these prevent the AA badge):**

1. **Lists are div-grids, not tables** (`entity-shell.tsx:267-333` + every list page). SR users get a flat reading order. SC 1.3.1.
2. **Forms never link errors to fields.** Zero `aria-invalid`/`aria-describedby` app-wide. SC 3.3.1, 3.3.3, 4.1.3.
3. **Muted-foreground contrast** fails AA. Dark `oklch(0.680 0 0)` × 0.6 alpha for placeholders/chevrons → ~2.8:1. Light `oklch(0.575 0 0)` × 0.5 alpha on `text-[10px]` micro-labels → ~2.3:1. SC 1.4.3.
4. **Icon-only buttons under 24×24** at `composer.tsx:524-536/609-621/677-688`, `channel-rail.tsx:131/228-237`, settings nav `Plus`. SC 2.5.8 (new in 2.2).
5. **Mention picker has `role="listbox"` but textarea isn't `role="combobox"`** — no `aria-controls`, no `aria-expanded`, no `aria-activedescendant`. SC 4.1.2.

**High-leverage quick wins:** `aria-current="page"` on NavLinks (5 min), `aria-label` on cmdk input + composer textarea (10 min), wire `aria-invalid`/`aria-describedby` on login + reset/forgot (1 h), make reaction picker close on Esc + outside-click (45 min), bump h-5 w-5 buttons (1 h sweep), `role="status"` (not `aria-live`) on RealtimeStatusPill (20 min), `role="log"` on Activity feed + chat MessageList (30 min), conditional render of empty `DialogDescription` (5 min), give Field required-dot an `sr-only` "required" sibling + `aria-required` (20 min), drop chat header `<h1>` to `<h2>` (5 min).

**Tooling recommendations:** Enable `jsx-a11y/no-noninteractive-element-interactions` as `error` after fixing the 3 outstanding warnings (`badges.tsx:303`, `avatar.tsx:94`, `brands.tsx:378`, `products.tsx:784`); add `@axe-core/react` in dev; add `@axe-core/playwright` sweep over login → / → /chat → /files → /settings/profile in CI.

### Dead code + unused exports

Codebase is **remarkably clean**: 0 TODO/FIXME/HACK, 0 debugger, 1 legitimate `console.warn` (`realtime-context.tsx:86`), no large commented-out blocks.

**Deletion candidates (cross-verified zero consumers):**

- Files: `components/file/file-gallery.tsx`, `components/sse/live-feed.tsx`, `components/sse/sse-status-badge.tsx`, `components/theme/theme-toggle.tsx` (the SSE folder becomes empty).
- List primitives: `density-toggle.tsx`, `empty-state.tsx`, `list-hero.tsx`, `pagination.tsx`, `sort-chips.tsx`, `stat.tsx` (prune `components/list/index.ts`).
- API functions: `getAuditsByTrace` (audits.ts:198), `reorderProductImages` (catalog.ts:353), `discoverChannels`/`restoreChannel` (chat.ts:86/117), `getRoleById` (identity.ts:147).
- Unused `export` keyword: `AuditTag` (audits.ts:47/57), `ChannelType` (chat.ts:4), `BRAND_LADDER` (appearance-options.ts:141), `shortenUserId` (chat-utils.ts:22), `ROOT_PASSWORD` (login.demo-accounts.ts:26), `useMobileNav` (mobile-nav.tsx:39), `badgeVariants` (badge.tsx:44), `buttonVariants` (button.tsx:83), `DialogTrigger`/`SheetTrigger`/`SheetClose` (dialog.tsx:24/191/192), `DropdownMenuGroup`/`DropdownMenuLinkItem` (dropdown-menu.tsx:17/75), `DropdownMenuPortal` (dropdown-menu.tsx:16).
- Unused npm deps: `@hookform/resolvers`, `react-hook-form`, `recharts`, `zod`, `@types/lodash`, `autoprefixer` (Tailwind v4 doesn't need it; there's no `postcss.config.*`).
- Unused custom CSS: `.chat-empty-hero` (globals.css:1298-1300).

**Important false-positives avoided:** `.accent-rose/.accent-indigo/.accent-violet/.accent-sky/.accent-emerald/.accent-amber` rules (globals.css:204-283) ARE used — applied dynamically via `accent-${id}` template strings in `index.html:63` bootstrap, `theme-provider.tsx:159`, `command-palette.tsx:259`.

**Tooling:** add `knip` (config: `entry: ["src/main.tsx", "tests/**/*.spec.ts"]`, `ignoreExportsUsedInFile: true`) as dev dep + CI gate; add `eslint-plugin-unused-imports` set to `error`.

### Performance + bundle

**Headline numbers (prod build):**

- Total JS gzip: **~411 KB** (375 main + 43 chat + ~25 other lazy).
- Cold-start JS (login → overview): **~204 KB**.
- Total CSS gzip: **20.3 KB** (single chunk, OK).
- Number of chunks: **78** (1 main + 1 CSS + 76 route/icon chunks).
- Largest chunk: `assets/index-BZ3i0yJ6.js` **196.7 KB gzip / 650 KB raw** (above 500 KB warning).
- Routes lazy: **31/31** — perfect.
- Estimated cold TTI on Fast 3G: **~3.5–4.5 s**.

**Main shell top contributors (gzip):** react-dom 96, react-router 46, app code 43, **@microsoft/signalr 37 (should be lazy)**, @tanstack/query 19, tailwind-merge 16, lucide 16, sonner 13, @radix-ui/* ~32, @floating-ui ~18, cmdk 4.6 (should be lazy), react-remove-scroll 4.7.

**Top 5 quick wins:**

1. **Lazy SignalR** → −37 KB main (~19% TTI).
2. **Move SseProvider + RealtimeProvider out of AppShell** — only mount on routes that subscribe (`/chat`, `/activity`, `/overview`, where notification-bell renders). Stops opening hub on settings/files/health/etc.
3. **Lazy CommandPaletteRoot** → −4.6 KB main + 12 KB raw; mount on first ⌘K.
4. **Split SseContext** so `OverviewPage`'s 1156-line tree doesn't re-render on every SSE event.
5. **Remove dead deps** (recharts, react-hook-form, @hookform/resolvers) from `package.json` — 120 KB install savings + future foot-gun prevention.

**TanStack Query hygiene:** 270 useQuery/useMutation calls across 43 files. Global `staleTime: 30_000` + `refetchOnWindowFocus: false` defaults at `lib/query-client.ts:13` are sound. `keepPreviousData` used in 7 list pages. No N+1 waterfalls found. `select` not used anywhere — could narrow MessageList subscription.

**Re-render hotspots:**

1. `SseContext` value identity changes on every event (`sse-context.tsx:186-189`) → `OverviewPage`/`LiveFeed`/`ActivityPage`/`notification-bell` all re-render.
2. `ThemeContext` value bundles 12 fields and setters (`theme-provider.tsx:321-336`) — every theme toggle re-renders every Avatar/Button/row touching `useTheme`.
3. **Zero `React.memo` usage app-wide** (`grep memo\(` returns nothing). Row components in long lists are prime candidates.
4. `RealtimeProvider` rebuilds the SignalR hub on every token change.
5. `ActivityPage` slices on every SSE event, no virtualization → 200 DOM rows update per event burst.

**Web Vitals risk register:**

- **LCP:** 12-font Google Fonts CSS request is render-blocking; main chunk 196 KB blocks initial render; overview hero waits on billing/usage queries.
- **INP:** ⌘K open mounts cmdk + palette synchronously; theme toggle via `flushSync` + view-transition; audits filter chip click triggers full re-fetch + summary re-fetch + 50-row re-render.
- **CLS:** Avatars without explicit width/height (`avatar.tsx:94`); late-loading impersonation banner; Google Fonts swap-in.

**Network/realtime cost:** API client is solid — AbortController + timeout + JWT refresh stampede protection. SignalR backoff `[2s, 5s, 10s, 30s]` then 60s cap with jitter. SSE backoff 1s → 30s exponential. Neither shares connections across tabs (every tab opens its own); add `BroadcastChannel` leader election in P2 for power users.

**Dev/prod parity:** No sourcemaps emitted (verified `ls dist/assets/*.map`). nginx caches hashed assets `1y immutable`, `index.html` no-cache, `config.json` no-store — canonical. Missing: CSP, HSTS, Permissions-Policy.

### UX patterns

**vs Linear / Vercel / Stripe / Cal.com / Resend:** strong primitives, materially behind on command palette scope, URL state, bulk actions, optimistic UI, inline editing, keyboard shortcuts, mobile-first patterns.

**Five cross-cutting capabilities to build once and deploy everywhere:**

1. `EntityBulkBar` + `useRowSelection<T>()` + checkbox column.
2. `useUrlState<T>()` hook (debounced sync of state ↔ search params).
3. `KeyboardShortcutsOverlay` + `useShortcut(key, handler)`.
4. `UndoToast` recipe (`toast.success(msg, { action: { label: "Undo" } })`).
5. `ConfirmDialog` with `dangerous`/`previewBlockquote`/`typeToConfirm` props (factored from chat delete + product delete + user delete dialogs).

**Per-surface highlights (representative; full audit lists 80+ findings):**

- **Overview:** Greeting refreshes on every render (`overview.tsx:923-933`); "View activity" + "View audits" duplicated 3 paths; first-run setup tiles never reflect completion; usage error tone identical to empty.
- **Identity Users:** No bulk actions; no CSV/SCIM invite; email-confirmation status shown but not actionable (no "Resend"); role-filter combobox lacks counts; password mismatch silent.
- **Identity User-detail:** Roles editor has no dirty-nav guard; sessions list shows IP but no geo; profile rows read-only when an operator commonly needs to fix typos; delete dialog doesn't require typing the email.
- **Role detail:** No per-resource preset (only Basic/All/Clear globally); tri-state toggles all permissions even when filtering.
- **Catalog Products:** No bulk-edit price/stock; filters not URL-synced; edit/delete on hover-only (broken on touch); no image upload in create dialog; SKU validation client-side only.
- **Tickets:** Status workflow invisible (no status picker); no SLA countdown; assignee/reporter shown as id-slice (`useUserDisplay` exists, unused); comment box is plain textarea (chat composer has all the features — port).
- **Files:** Drag-drop only inside dropzone, not global; no folders; no bulk download/delete/move/share; share dialog doesn't exist (only public/private toggle); no keyboard nav between files.
- **Chat:** No emoji picker beyond 6 quick-reactions; composer plain-text only; threads only show reply context inline, no inline expand under parent (Mukesh's preferred Teams pattern not yet implemented per the memory note); `markRead` fires even when user is scrolled away; reaction toggle is round-tripped, not optimistic; no image lightbox.
- **Invoices:** No filter pills, no PDF download, no detail drill-in, tenant chip is dead code.
- **Audits:** Filters not URL-synced; no correlation-id deep link from row; no JSON diff for `EntityChange` events; no realtime mode despite SSE; no saved views; no CSV export; severity tone identical for Error and Critical.
- **Health:** 24-tick ring buffer (2 min at 5s poll) — bump to 360 for 30 min; no alert subscription; no per-check runbook URL.
- **System Sessions:** No per-user revoke; no filter for current device / expiring soon; no geo.
- **System Trash:** No bulk-restore; no "permanent delete" (GDPR scenario); tab counts hidden; no auto-purge policy display.
- **Settings:** `notifications` and `api-keys` are placeholders ("coming soon") — hide or move to a disclosure; password change doesn't invalidate other sessions; 2FA enroll doesn't show recovery codes; no preview pane in Appearance.
- **Auth:** Tenant is free-text (subdomain detection / recent tenants dropdown is the norm); no OIDC provider buttons despite stack support.
- **Shell / palette / navigation:** Palette is navigation-only and misses Identity/Catalog/Tickets/Files/Chat; no Create group; no `?` overlay; no breadcrumbs; sidebar accordion single-select; no tenant switcher; mobile is drawer-only (not bottom tabs).
- **Toasts:** Zero use of `action` prop in 153 `toast.*` calls — no undo pattern; errors that need recovery routed to toasts (auto-dismiss in 4.2s, user loses context).
- **Realtime:** `usePresence` only feeds chat avatars; user pickers/ticket-assignee/sessions/audit user columns all show names without presence dots.
- **RouteError:** Dumps `error.stack` to end users (should be DEV-only); no offline detection; no token-expired UX (just boots to login with no message).

### Packages + security

- **Total deps:** 530 transitive in dashboard (170 prod + 361 dev + 76 opt); 389 in admin.
- **Outdated:** 22 in dashboard, 20 in admin.
- **Security advisories:** 1 moderate, transitive (`brace-expansion` via `@typescript-eslint/typescript-estree`), auto-fixable with `npm audit fix`. Zero high/critical.
- **Cross-app version drift: ZERO.** Every shared package resolves to the identical installed version. Major win.

**Wave 1 (zero risk, do now — one PR):** react 19.2.6, @tanstack/react-query 5.100.11, @tanstack/react-virtual 3.13.25 (dashboard only), react-router 7.15.1, react-hook-form 7.76 (only if kept), typescript-eslint 8.59.4, tailwindcss 4.3.0 (+ @tailwindcss/vite 4.3.0 lockstep), tailwind-merge 3.6, vite 7.3.3 (NOT v8 yet), `npm audit fix`.

**Wave 3 (one PR per major):**

| Order | Pkg | Effort | Notes |
|---|---|---|---|
| 1 | `@vitejs/plugin-react` 4 → 6 | 1–2 h | Drop-in; needs Vite 7+. |
| 2 | `eslint-plugin-react-hooks` 5 → 7 | 1 h | Will surface new compiler-aware warnings. |
| 3 | `eslint-plugin-react-refresh` 0.4 → 0.5 | 30 min | Config compatible. |
| 4 | `globals` 15 → 17 | 5 min | Data package. |
| 5 | `lucide-react` 0.475 → 1.16 | 2–3 h | First stable 1.x; renamed icons; do as own PR. |
| 6 | `@hookform/resolvers` 3 → 5 (admin only) | 1 h | API stable for `zodResolver(schema)`. |
| 7 | `zod` 3 → 4 (admin only) | 2–4 h | Significant rewrite; touch every schema. |
| 8 | `recharts` 2 → 3 (dashboard) | 3–5 h | Only after we decide whether to keep it (1 file uses it). |

**Holds (await ecosystem):** vite 8 (released today, 2026-05-21 — wait 2–4 weeks for plugin certification), typescript 6 (await typescript-eslint 9), eslint 10 (await typescript-eslint 9), @types/node 25 (pin to Node 22 LTS runtime).

**Recommended Renovate config** at repo root (`renovate.json`) — groups patch/minor + auto-merges, holds majors for review, dedicated groups for `@radix-ui/*` and `@tanstack/*`, weekly lockfile maintenance, security auto-merge:

```json
{
  "$schema": "https://docs.renovatebot.com/renovate-schema.json",
  "extends": ["config:recommended", ":semanticCommits", ":dependencyDashboard"],
  "timezone": "Asia/Kolkata",
  "schedule": ["before 4am on monday"],
  "labels": ["dependencies"],
  "packageRules": [
    { "matchManagers": ["npm"], "matchFileNames": ["clients/**"],
      "groupName": "frontend patch & minor",
      "matchUpdateTypes": ["patch", "minor"],
      "automerge": true, "automergeType": "pr", "platformAutomerge": true },
    { "matchManagers": ["npm"],
      "matchPackageNames": ["react","react-dom","@types/react","@types/react-dom","vite","@vitejs/plugin-react","tailwindcss","@tailwindcss/vite","typescript","eslint","typescript-eslint","@hookform/resolvers","zod","recharts","lucide-react"],
      "matchUpdateTypes": ["major"],
      "groupName": "frontend majors (manual review)",
      "automerge": false, "labels": ["dependencies","major-bump","review-required"] },
    { "matchManagers": ["npm"], "matchPackagePatterns": ["^@radix-ui/"], "groupName": "radix-ui", "automerge": true },
    { "matchManagers": ["npm"], "matchPackagePatterns": ["^@tanstack/"], "groupName": "tanstack", "automerge": true }
  ],
  "vulnerabilityAlerts": { "labels": ["security"], "automerge": true },
  "lockFileMaintenance": { "enabled": true, "automerge": true, "schedule": ["before 4am on monday"] }
}
```

**Strategic:** worth moving `clients/` to a pnpm workspace (4–6 h effort, high long-term ROI — single resolved copy of React/Radix/Tailwind/Vite, no future drift, makes a shared `packages/ui` trivial).

---

## Suggested PR sequence (next 2 weeks)

1. **`chore: dashboard dependency hygiene`** — `npm audit fix`, Wave-1 bumps, remove dead deps (recharts/react-hook-form/@hookform/resolvers/zod/@types/lodash/autoprefixer). Verify `npm run build` + `npm run lint` + Playwright. ~1 h.
2. **`chore: dashboard dead code cleanup`** — delete the 4 orphan files + 6 dead list primitives + prune `components/list/index.ts`. ~30 min.
3. **`perf(dashboard): lazy SignalR + scope realtime providers + lazy cmdk`** — −41 KB main shell. ~2–3 h.
4. **`perf(dashboard): cull eager fonts to 3`** — 200–400 KB cold-load savings. ~1 h.
5. **`feat(dashboard): command palette navigates everywhere + Create group`** — biggest perceived-completeness win. ~2 h.
6. **`a11y(dashboard): aria-current, aria-label, focus indicators, target sizes`** — bundle the dozen quick wins. ~3 h.
7. **`style(dashboard): type scale + transitionDuration tokens + drop backdrop-grayscale + light-mode elevation + brand gradient`** — visual cohesion pass. ~2 h.
8. **`feat(dashboard): URL-synced filters on /audits (template)`** — then roll to other lists in subsequent PRs. ~6 h template + 2 h/page.
9. **`feat(dashboard): EntityBulkBar primitive`** — then roll to Sessions, Trash, Users, Products. ~1 d primitive + 0.5 d/page.
10. **`feat(dashboard): keyboard shortcuts overlay + global hotkeys`** — `?`, `c`, `/`, `j/k`. ~1 d.

---

## Acceptance signals (when do we call it "world-class")

- **Lighthouse / PageSpeed:** Performance ≥ 90 desktop / ≥ 80 mobile, Accessibility ≥ 95, Best-Practices ≥ 95.
- **`@axe-core/playwright`:** 0 serious/critical violations on login → / → /chat → /files → /settings/profile → /identity/users.
- **Bundle:** main chunk ≤ 160 KB gzip; cold-start JS ≤ 170 KB.
- **TTI Fast 3G:** ≤ 3.0 s.
- **WCAG 2.2 AA:** all five named blockers cleared.
- **UX:** ⌘K covers every route + create + record search; every list has URL-synced filters; bulk-action bar shipped on ≥ 5 lists; undo-toast pattern on every destructive default; `?` overlay shipped.
- **Dependencies:** zero outstanding `npm audit` issues; Wave-1 + Wave-3 (P0 majors) merged; Renovate live; ≤ 5 deps more than one minor behind latest.

---

## Source references

Every claim in this audit traces to file:line in `clients/dashboard/`. The compressed dimension summaries above are pointers; treat the audit as a coordinate system, not the full record. Full per-dimension findings live in the session's six agent outputs; bring them back via `git log` / future audit re-runs if needed.

Key file paths cited across dimensions:

- Layout: `src/components/layout/{app-shell, sidebar, topbar, mobile-nav, impersonation-banner}.tsx`
- UI primitives: `src/components/ui/{avatar, badge, button, card, dialog, dropdown-menu, input, label, skeleton, switch}.tsx`
- List primitives (5 of 13 dead): `src/components/list/{page-hero, list-hero, entity-shell, entity-detail, sort-chips, density-toggle, pagination, stat, empty-state, field, combobox, error-band, tone-icon-tile}.tsx` + `index.ts`
- Pages: `src/pages/**/*.tsx` (40 files; see full agent reports for per-page line citations)
- Auth: `src/auth/{auth-context, protected-route, jwt, token-store, api}.tsx|.ts`
- Realtime/SSE: `src/realtime/{realtime-context, use-presence}.tsx|.ts`, `src/sse/{sse-context, sse-api}.tsx|.ts`
- API client: `src/lib/api-client.ts`, `src/lib/query-client.ts`
- Tokens: `src/styles/globals.css` (1326 lines)
- Theme: `src/components/theme/{theme-provider, appearance-options}.tsx|.ts`
- Build/serve: `vite.config.ts`, `docker/nginx.conf`, `index.html`, `eslint.config.js`, `package.json`

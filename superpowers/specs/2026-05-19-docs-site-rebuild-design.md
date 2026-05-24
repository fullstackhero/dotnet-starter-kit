---
title: Docs site rebuild (Astro + Tailwind 4, blog visual twin)
date: 2026-05-19
status: approved
owner: iammukeshm
---

# Overview

Rebuild the FullStackHero starter-kit documentation site from scratch as a fully custom Astro site that visually mirrors the codewithmukesh blog (`C:\Users\mukesh\repos\codewithmukesh\blog`). The site combines a marketing landing page (`/`) with an MDX-backed documentation tree (`/docs/*`). Single-trunk versioning, Orama client-side search, Cloudflare Pages deploy.

Brand primary changes from the blog's purple `#4c33d8` to green `#15803d`, with `#16a34a` carried in `--primary-soft` so the user-specified brand green surfaces in accent text, tinted backgrounds, and the gradient mid-stop. Every other design token, prose rule, code-block theme, and shell idiom is copied from the blog verbatim.

# Goals

- Pixel-identical look-and-feel to codewithmukesh/blog (typography, surfaces, prose, code blocks, scrollbar, theme toggle).
- Marketing landing page + MDX-backed docs in one Astro project.
- Hand-written MDX content across six top-level sections.
- Cloudflare Pages deploy, mirroring the blog's operational pattern.
- Client-side Orama search scoped to `/docs/*`.
- WCAG AA contrast on every primary-color usage in both themes.

# Non-goals

- Versioned docs (single trunk; layer versioning in later when v2 ships).
- Auto-generated API reference or OpenAPI / Scalar embed.
- Newsletter forms, contact forms, PostHog analytics in v1.
- Internationalization.
- Contributor on-boarding docs beyond a short pointer to repo `CONTRIBUTING.md`.

# Tech stack

| Concern | Pick | Notes |
|---|---|---|
| Framework | Astro (latest stable: 5.x; track 6.x if released) | SSG output |
| Adapter | `@astrojs/cloudflare` | matches blog deploy |
| CSS | Tailwind 4.x via `@tailwindcss/vite` | same plugin model as blog |
| MDX | `@astrojs/mdx` | content collection for `/docs/*` |
| Code blocks | `astro-expressive-code` with Houston + github-light | config copied verbatim, color overrides re-tinted green |
| Icons | `astro-icon` + `@iconify-json/lucide` | same icon set as blog |
| Search | `@orama/plugin-astro` | `pathMatcher: /^\/docs\//` |
| React islands | `@astrojs/react` + React 19 | only for `ThemeToggle`, `SearchModal` |
| Fonts | Astro experimental fonts: Outfit + Figtree + JetBrains Mono | self-hosted via Google provider |
| Sitemap | `@astrojs/sitemap` | includes `/`, `/docs/**`; excludes `sidebar.hidden` pages |
| Reading-time | dropped (low value for reference content) | — |
| Modified-time | `remark-modified-time.mjs` copied from blog | drives "Last updated" footer on docs pages |
| Package manager | npm | matches starter-kit repo (blog uses bun) |
| Node | `>=20.0.0` | |

# Information architecture

Two surfaces sharing the same shell (Header, Footer, theme toggle, paper-grain body texture):

**Marketing** — no sidebar:

- `/` — landing (hero, features, install snippet, CTAs to docs and GitHub)

**Docs** — sidebar + TOC visible:

- `/docs/` — docs home (welcome, three-pillar quick links)
- `/docs/getting-started/` — install, first run, project tour, deploy targets
- `/docs/concepts/` — architecture (modular monolith + VSA), modules & contracts, CQRS+Mediator, persistence, multitenancy, eventing, security
- `/docs/modules/` — per-module deep dives (Identity, Multitenancy, Auditing)
- `/docs/recipes/` — task-oriented how-tos (add a feature, add a module, write an integration test, configure storage, add a tenant)
- `/docs/reference/` — config keys, CLI commands, permission constants, exception types
- `/docs/contributing/` — short pointer to repo `CONTRIBUTING.md`

Header nav: **Docs** (→ `/docs`), **GitHub** (external), search trigger, theme toggle, mobile drawer.

# Routing & content model

Single dynamic route `src/pages/docs/[...slug].astro` renders every MDX file in the `docs` content collection. File path under `src/content/docs/` maps directly to URL.

```
src/content/docs/index.mdx                          → /docs/
src/content/docs/getting-started/install.mdx        → /docs/getting-started/install/
src/content/docs/modules/identity/index.mdx         → /docs/modules/identity/
src/content/docs/modules/identity/users.mdx         → /docs/modules/identity/users/
```

**Content collection schema** (`src/content.config.ts`):

```ts
import { defineCollection, z } from 'astro:content';
import { glob } from 'astro/loaders';

const docs = defineCollection({
  loader: glob({ pattern: '**/*.mdx', base: './src/content/docs' }),
  schema: z.object({
    title: z.string(),
    description: z.string(),
    sidebar: z.object({
      label: z.string().optional(),
      order: z.number().default(999),
      hidden: z.boolean().default(false),
    }).default({}),
    pageType: z.enum(['guide', 'reference', 'concept', 'recipe']).default('guide'),
    lastUpdated: z.date().optional(),  // injected by remark-modified-time
  }),
});

export const collections = { docs };
```

**Sidebar tree** is derived from file structure by `src/helpers/sidebar.ts`:

- Each top-level directory under `content/docs/` becomes a section.
- Section display names + order come from a tiny `src/content/docs/_sections.ts`:
  ```ts
  export const sections = [
    { dir: 'getting-started', label: 'Getting Started', order: 1 },
    { dir: 'concepts',        label: 'Concepts',        order: 2 },
    { dir: 'modules',         label: 'Modules',         order: 3 },
    { dir: 'recipes',         label: 'Recipes',         order: 4 },
    { dir: 'reference',       label: 'Reference',       order: 5 },
    { dir: 'contributing',    label: 'Contributing',    order: 6 },
  ];
  ```
- Page order within a section uses `sidebar.order`; ties broken alphabetically by filename.
- A directory's `index.mdx` becomes the section landing page.
- Pages with `sidebar.hidden: true` are excluded from the sidebar (still routable).

**Search**: Orama indexes only pages matching `/^\/docs\//`. Content selectors `["h1", "h2", "[data-search-meta]"]`. Triggered via `Ctrl/Cmd+K` and the header search button.

**Redirects**: none. Clean slate URL-wise.

# Design tokens — copy-verbatim, swap primary

Forked from `C:\Users\mukesh\repos\codewithmukesh\blog\src\styles\` on 2026-05-19. Each file carries a provenance comment at the top: `/* Forked from codewithmukesh/blog 2026-05-19. Keep in sync manually until shared package extracted. */`

**Files copied verbatim** into `docs/src/styles/`:

- `base.css` — light + dark token sets, table/blockquote rules, Catppuccin block.
- `brand-typography.css` — type scale.
- `prose.css` — long-form prose styling (this *is* the docs reading experience).
- `globals.css` — `@theme inline` bridge, dark-mode primary routing, paper-grain `body::before`, scrollbar, selection, skip-link, `gradient-text`, `section-dots`, `magnetic-shimmer`, `brand-shadow`, view-transition theme flip.
- `loader.css` — kept; cheap.

**Files modified after copy**:

- `base.css` — `--primary`, `--primary-hover`, `--primary-soft`, `--ring` re-tinted green (see below). `:root` keeps the warm-paper neutrals untouched.
- `brand-tokens.css` — `--gradient-brand` re-tinted green.
- `globals.css` — strip the two `@plugin` lines (`tailwindcss-animate`, `@tailwindcss/forms`). Forms aren't used and the accordion keyframes are already defined inline in the `@theme inline` block. This avoids carrying two npm deps we don't need.

**Files skipped**:

- `formkit.css` — no forms in v1.

**Primary color tokens**:

| Token | Light | Dark | Contrast notes |
|---|---|---|---|
| `--primary` | `#15803d` | `#15803d` | green-700. White on this = 4.49:1 (AA passes). |
| `--primary-hover` | `#166534` | `#166534` | green-800. Darker for hover state. |
| `--primary-soft` | `#16a34a` | `#4ade80` | Light = the user-specified brand green for tinted bgs. Dark = green-400, brighter so `.text-primary` reroute hits AA on dark cards. |
| `--ring` | `#15803d` | `#15803d` | Focus rings. |
| `--gradient-brand` | `linear-gradient(135deg, #15803d 0%, #16a34a 50%, #4ade80 100%)` | same | Brand-signature gradient. |

**Astro config — Expressive Code overrides** (in `astro.config.mjs`): every hardcoded `#4c33d8` in the blog's EC config becomes `#15803d`; every `rgba(76, 51, 216, X)` becomes `rgba(21, 128, 61, X)`. Affected keys: `focusBorder`, `editorActiveTabIndicatorTopColor`, `inlineButtonBackground`, `inlineButtonBackgroundHover`, `inlineButtonForeground`, `markBackground`, `markBorderColor`. Tab/frame neutrals and Houston/github-light theme refs stay untouched.

**Astro config — fonts**: experimental fonts block copied verbatim (Outfit, Figtree, JetBrains Mono) with identical weights, subsets, fallbacks, and CSS variable names.

**Astro config — image, prefetch, vite ssr/build, markdown** blocks copied verbatim where applicable; Cloudflare-specific aliases retained for React 19 SSR (`react-dom/server.edge`).

# Layouts and components

## Layouts (`src/layouts/`)

- `BaseLayout.astro` — root. Loads global CSS, fonts, theme-init script, paper-grain, view-transitions, `<html lang>`, head SEO meta. Used by every page.
- `MarketingLayout.astro` — `BaseLayout` + Header + Footer; no sidebar. Used by `/`.
- `DocsLayout.astro` — `BaseLayout` + Header + Sidebar + main + TOC + Footer. Used by `/docs/[...slug]`.

## Shell components (`src/components/shell/`)

Visual twins of blog header/footer; routes rewired for docs.

- `Header.astro` — wordmark, nav (Docs / GitHub), search trigger, theme toggle, mobile drawer button. Header heights driven by `--spacing-header` / `--spacing-header-lg` from blog tokens.
- `Footer.astro` — copyright, repo link, license link, link to `fullstackhero.net`.
- `ThemeToggle.tsx` — React island, View Transitions API flip (copied verbatim from blog).
- `MobileDrawer.astro` — same `fadeSlideUp` animation idiom from globals.css.
- `SkipToContent.astro` — keyboard-only skip link.

## Docs components (`src/components/docs/`)

- `Sidebar.astro` — file-derived section tree, current-page highlight, collapsible sections, sticky on lg+, drawer on mobile.
- `TableOfContents.astro` — built from page H2/H3 via Astro `headings` prop. Sticky on the right at xl+, hidden below xl.
- `PageHeader.astro` — title, description, `pageType` badge, "Last updated".
- `PrevNext.astro` — auto-computed prev/next within the same section.
- `Breadcrumbs.astro` — derived from slug.
- `SearchTrigger.astro` + `SearchModal.tsx` — Orama-powered, `Ctrl/Cmd+K` shortcut, styled to match blog modal idiom.
- `Callout.astro` — `<Callout type="note|warning|tip|danger">`. Uses semantic tokens (`--accent`, `--warning`, `--destructive`, `--info`).
- `CodeGroup.astro` — tabbed code blocks (e.g. PowerShell / Bash).

## Landing components (`src/components/landing/`)

- `Hero.astro` — gradient headline via `.gradient-text`, one-line pitch, primary CTA → `/docs/getting-started/install`, secondary CTA → GitHub. `brand-shadow` accent.
- `FeatureGrid.astro` — `magnetic-shimmer` cards on hover.
- `InstallSnippet.astro` — single Expressive Code block with copy button.
- `FinalCta.astro` — repo + docs links.

## MDX component map (`src/components/mdx.ts`)

Passed to `<Content components={...} />` on every docs page:

- `Callout`, `CodeGroup` from `components/docs/`.
- Custom `<a>` (external-link icon for off-site links).
- Custom `<table>` wrapper for horizontal scroll on narrow viewports.
- Custom `<h2>`/`<h3>` rendering anchor links; the existing `:target::before` highlight in globals.css picks up the navigation.

## Scripts (`src/scripts/`)

- `theme-init.js` — inline `<head>` script reading `localStorage`, applies `.dark` before first paint (no flash).
- `motion-reveal.ts` — IntersectionObserver hook for `[data-reveal]` (already styled in globals.css).

# Directory layout (final)

```
docs/
├── astro.config.mjs
├── package.json
├── tsconfig.json
├── wrangler.toml
├── .gitignore                  # node_modules, .astro, dist, .wrangler
├── README.md                   # how to run/build locally
├── public/
│   ├── favicon.svg
│   └── og-default.png
└── src/
    ├── content/
    │   ├── docs/
    │   │   ├── index.mdx
    │   │   ├── _sections.ts
    │   │   ├── getting-started/
    │   │   ├── concepts/
    │   │   ├── modules/
    │   │   ├── recipes/
    │   │   ├── reference/
    │   │   └── contributing/
    ├── content.config.ts
    ├── components/
    │   ├── shell/
    │   ├── docs/
    │   ├── landing/
    │   └── mdx.ts
    ├── data/
    │   └── site.ts
    ├── layouts/
    │   ├── BaseLayout.astro
    │   ├── MarketingLayout.astro
    │   └── DocsLayout.astro
    ├── pages/
    │   ├── index.astro
    │   ├── docs/[...slug].astro
    │   └── 404.astro
    ├── scripts/
    │   ├── theme-init.js
    │   └── motion-reveal.ts
    ├── styles/                 # forked from blog
    │   ├── base.css
    │   ├── brand-tokens.css
    │   ├── brand-typography.css
    │   ├── globals.css
    │   ├── loader.css
    │   └── prose.css
    ├── remark/
    │   └── remark-modified-time.mjs
    ├── helpers/
    │   └── sidebar.ts
    └── env.d.ts
```

# Build & deploy

**Scripts** (`package.json`):

```json
"scripts": {
  "dev": "astro dev",
  "start": "astro dev",
  "build": "astro build",
  "preview": "astro preview",
  "check": "astro check",
  "astro": "astro"
}
```

**Cloudflare Pages**:

- Adapter: `@astrojs/cloudflare`, `imageService: 'compile'`.
- `wrangler.toml` with `pages_build_output_dir = "dist"`.
- Build command: `npm ci && npm run build`.
- Node: `>=20.0.0`.
- Branch deploys: `main` → production, others → preview.

**Site config** (`src/data/site.ts`):

```ts
export default {
  url: 'https://docs.fullstackhero.net',
  title: 'FullStackHero — .NET Starter Kit',
  description: 'Production-ready modular .NET starter kit. Modular monolith + VSA, multitenancy-first, identity built in.',
  repo: 'https://github.com/fullstackhero/dotnet-starter-kit',
  author: 'Mukesh Murugan',
  ogImage: '/og-default.png',
};
```

(URL is the chosen default; trivial to change later via this file plus `site:` in `astro.config.mjs`.)

**Local dev**:

```bash
cd docs
npm install
npm run dev
```

# Open items

- Production URL — set to `https://docs.fullstackhero.net` by default. Change in `src/data/site.ts` if a different host is chosen.
- Whether to wire PostHog later (deferred; intentionally out of v1 scope).
- Initial MDX page bodies (covered by the implementation plan, not the design).
- Shared-package extraction of design tokens between blog and docs (deferred — manual sync until the duplication becomes painful).

# Risks

- **Token drift** between blog and docs. Mitigation: provenance comment + periodic diff against blog `src/styles/`. If drift becomes a problem, extract a shared `@codewithmukesh/design-tokens` package.
- **Expressive Code color override surface** is wide. Mitigation: every hardcoded purple is enumerated in this spec; lint test could grep for `#4c33d8` and `rgba(76, 51, 216` post-build to catch any missed instance.
- **React 19 + Cloudflare SSR** — known footgun documented in blog's `astro.config.mjs` (`react-dom/server.edge` alias). The same alias is included here.
- **Astro version target** — "latest" floats. We pin in `package.json` at install time and accept the floor of `^5.18.0` (or `^6.0.0` if 6.x is GA on install day).

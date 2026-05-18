# Docs Site Rebuild Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a fresh Astro + Tailwind 4 docs site under `docs/` for the FullStackHero starter kit, visually identical to the codewithmukesh blog, with a marketing landing at `/` and an MDX-backed docs tree at `/docs/*`.

**Architecture:** Fully custom Astro site (no Starlight). Two surfaces share one shell (Header/Footer/theme toggle). Docs content is hand-written MDX in a single content collection; sidebar is derived from file structure. Brand primary swapped from blog purple to green (`#15803d`), user-specified `#16a34a` lives in `--primary-soft`. Cloudflare Pages deploy, Orama client-side search scoped to `/docs/*`.

**Tech Stack:** Astro 5.x (or 6.x if GA on install day), Tailwind 4 via `@tailwindcss/vite`, MDX, React 19 islands, `astro-expressive-code`, `@orama/plugin-astro`, `@astrojs/cloudflare`, `@astrojs/sitemap`, `astro-icon` + `@iconify-json/lucide`, self-hosted Outfit + Figtree + JetBrains Mono fonts.

**Source of truth for visual reuse:** `C:\Users\mukesh\repos\codewithmukesh\blog\` (referred to as `BLOG/` below). When a task says "mirror BLOG/path/X.astro", read that file and reproduce its DOM/class structure as a visual twin, then adapt navigation/links/copy for the docs site. Token CSS files are copied verbatim with surgical diffs documented per task.

**Working directory:** All paths in this plan are relative to `C:\Users\mukesh\repos\fullstackhero\dotnet-starter-kit\`.

**Reference design spec:** `docs/superpowers/specs/2026-05-19-docs-site-rebuild-design.md` (commit `5c00d18b`).

**Note on testing:** This is a static site rebuild. "Tests" in this plan are explicit `dev` server checks and `astro build` runs that must succeed cleanly. Where useful, a Playwright smoke check is included at the end. There are no unit tests for component visuals — that's verified by eye against the blog.

---

## Task 1: Scaffold docs/ project files (package.json, tsconfig, gitignore)

**Files:**
- Create: `docs/package.json`
- Create: `docs/tsconfig.json`
- Create: `docs/.gitignore`

- [ ] **Step 1: Create `docs/package.json`**

```json
{
  "name": "fullstackhero-docs",
  "type": "module",
  "version": "0.1.0",
  "private": true,
  "engines": { "node": ">=20.0.0" },
  "overrides": {
    "react": "^19.2.4",
    "react-dom": "^19.2.4"
  },
  "scripts": {
    "dev": "astro dev",
    "start": "astro dev",
    "build": "astro build",
    "preview": "astro preview",
    "check": "astro check",
    "astro": "astro"
  }
}
```

- [ ] **Step 2: Create `docs/tsconfig.json`**

```json
{
  "extends": "astro/tsconfigs/strict",
  "include": [".astro/types.d.ts", "**/*"],
  "exclude": ["dist"],
  "compilerOptions": {
    "jsx": "react-jsx",
    "jsxImportSource": "react"
  }
}
```

- [ ] **Step 3: Create `docs/.gitignore`**

```
node_modules/
dist/
.astro/
.wrangler/
.env
.env.local
.DS_Store
*.log
```

- [ ] **Step 4: Commit**

```bash
git add docs/package.json docs/tsconfig.json docs/.gitignore
git commit -m "feat(docs): scaffold project files (package.json, tsconfig, gitignore)"
```

---

## Task 2: Install dependencies

**Files:** updates `docs/package.json` + creates `docs/package-lock.json`.

- [ ] **Step 1: Install runtime dependencies**

Run from `docs/`:

```bash
cd docs
npm install astro @astrojs/cloudflare @astrojs/mdx @astrojs/react @astrojs/sitemap @tailwindcss/vite tailwindcss astro-expressive-code @expressive-code/plugin-line-numbers astro-icon @iconify-json/lucide @orama/plugin-astro react react-dom @types/react @types/react-dom typescript clsx tailwind-merge class-variance-authority lucide-react @radix-ui/react-dialog
```

- [ ] **Step 2: Install dev dependencies**

```bash
npm install -D @astrojs/check @types/node prettier prettier-plugin-astro prettier-plugin-tailwindcss
```

- [ ] **Step 3: Verify package.json populated and lockfile generated**

Run `npm ls --depth=0` — expect a clean list with no peer warnings beyond known React 19 noise.

- [ ] **Step 4: Commit**

```bash
git add docs/package.json docs/package-lock.json
git commit -m "feat(docs): install astro + tailwind 4 + integrations"
```

---

## Task 3: Create directory skeleton

**Files:** creates empty dirs (use `.gitkeep` for any dir that would otherwise be empty at commit time).

- [ ] **Step 1: Create directory tree**

```bash
mkdir -p docs/public docs/src/content/docs/{getting-started,concepts,modules,recipes,reference,contributing}
mkdir -p docs/src/{components/{shell,docs,landing},data,layouts,pages/docs,scripts,styles,remark,helpers}
```

- [ ] **Step 2: Drop a `.gitkeep` in `docs/public/` for now** (will be replaced by favicon/og-default in Task 23):

Create empty file `docs/public/.gitkeep`.

- [ ] **Step 3: Commit**

```bash
git add docs/public docs/src
git commit -m "feat(docs): create directory skeleton"
```

---

## Task 4: Copy design-token CSS files from blog (verbatim, with provenance)

**Files:**
- Create: `docs/src/styles/base.css`
- Create: `docs/src/styles/brand-tokens.css`
- Create: `docs/src/styles/brand-typography.css`
- Create: `docs/src/styles/globals.css`
- Create: `docs/src/styles/prose.css`
- Create: `docs/src/styles/loader.css`

- [ ] **Step 1: Copy each file verbatim from blog**

For each file in `{base,brand-tokens,brand-typography,globals,prose,loader}.css`:

```powershell
Copy-Item "C:\Users\mukesh\repos\codewithmukesh\blog\src\styles\<file>.css" "docs\src\styles\<file>.css"
```

- [ ] **Step 2: Prepend provenance comment to each copied file**

At the very top of each of the six files (before any existing comment), add:

```css
/* Forked from codewithmukesh/blog src/styles/<filename>.css on 2026-05-19.
   Keep in sync manually until shared package extracted. */
```

- [ ] **Step 3: Commit (verbatim copies, no behavior changes yet)**

```bash
git add docs/src/styles/
git commit -m "feat(docs): import design tokens verbatim from codewithmukesh/blog"
```

---

## Task 5: Rebrand primary token to green (light + dark)

**Files:** `docs/src/styles/base.css`

- [ ] **Step 1: In the `:root` block, swap four lines**

Old (light mode):
```css
    --primary: #4c33d8;
    --primary-foreground: #ffffff;
    --primary-hover: #3f29b3;
    --primary-soft: #9682fb;
```
…and later in `:root`:
```css
    --ring: #4c33d8;
```

New:
```css
    --primary: #15803d;
    --primary-foreground: #ffffff;
    --primary-hover: #166534;
    --primary-soft: #16a34a;
```
…and:
```css
    --ring: #15803d;
```

- [ ] **Step 2: In the `.dark` block, swap four lines**

Old:
```css
    --primary: #6850e8;
    --primary-foreground: #ffffff;
    --primary-hover: #5942d8;
    --primary-soft: #a596fb;
```
…and:
```css
    --ring: #4c33d8;
```

New:
```css
    --primary: #15803d;
    --primary-foreground: #ffffff;
    --primary-hover: #166534;
    --primary-soft: #4ade80;
```
…and:
```css
    --ring: #15803d;
```

- [ ] **Step 3: Verify no other `#4c33d8` / `#6850e8` / `#3f29b3` / `#5942d8` / `#9682fb` / `#a596fb` remain in `base.css`**

Run:
```bash
grep -nE "#(4c33d8|6850e8|3f29b3|5942d8|9682fb|a596fb)" docs/src/styles/base.css
```
Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add docs/src/styles/base.css
git commit -m "feat(docs): swap primary token to green (#15803d / soft #16a34a)"
```

---

## Task 6: Rebrand gradient and strip unused plugin imports

**Files:**
- Modify: `docs/src/styles/brand-tokens.css`
- Modify: `docs/src/styles/globals.css`

- [ ] **Step 1: In `brand-tokens.css`, replace the gradient line**

Old:
```css
  --gradient-brand: linear-gradient(135deg, #4c33d8 0%, #7659ec 50%, #9682fb 100%);
```

New:
```css
  --gradient-brand: linear-gradient(135deg, #15803d 0%, #16a34a 50%, #4ade80 100%);
```

- [ ] **Step 2: In `globals.css`, delete two `@plugin` lines**

Remove these two lines near the top:
```css
@plugin 'tailwindcss-animate';
@plugin '@tailwindcss/forms';
```

The accordion keyframes in `@theme inline` stay (they're inlined, not from the plugin). Forms plugin is unused.

- [ ] **Step 3: Verify**

```bash
grep -n "@plugin" docs/src/styles/globals.css
```
Expected: no output.

```bash
grep -nE "(#4c33d8|#7659ec|#9682fb)" docs/src/styles/brand-tokens.css
```
Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add docs/src/styles/brand-tokens.css docs/src/styles/globals.css
git commit -m "feat(docs): rebrand gradient to green; drop unused tailwind plugins"
```

---

## Task 7: Write `astro.config.mjs` with integrations and green Expressive Code overrides

**Files:**
- Create: `docs/astro.config.mjs`
- Create: `docs/src/env.d.ts`
- Create: `docs/houston.theme.json` (copied from blog for EC dark theme)

- [ ] **Step 1: Copy Houston theme**

```powershell
Copy-Item "C:\Users\mukesh\repos\codewithmukesh\blog\houston.theme.json" "docs\houston.theme.json"
```

- [ ] **Step 2: Create `docs/src/env.d.ts`**

```ts
/// <reference types="astro/client" />
```

- [ ] **Step 3: Create `docs/astro.config.mjs`**

This mirrors the blog's config but: drops partytown/posthog/orama-with-blog-pathMatcher (orama is re-added with docs pathMatcher), removes RSS, sets the docs site URL, and re-tints every hardcoded purple to green.

```js
import cloudflare from '@astrojs/cloudflare';
import mdx from '@astrojs/mdx';
import react from '@astrojs/react';
import sitemap from '@astrojs/sitemap';
import tailwindcss from '@tailwindcss/vite';
import { pluginLineNumbers } from '@expressive-code/plugin-line-numbers';
import orama from '@orama/plugin-astro';
import astroExpressiveCode from 'astro-expressive-code';
import icon from 'astro-icon';
import { defineConfig, fontProviders } from 'astro/config';
import houston from './houston.theme.json';
import { remarkModifiedTime } from './src/remark/remark-modified-time.mjs';
import siteConfig from './src/data/site';

export default defineConfig({
  site: siteConfig.url,
  image: {
    layout: 'constrained',
    responsiveStyles: true,
  },
  prefetch: {
    prefetchAll: false,
    defaultStrategy: 'hover',
  },
  integrations: [
    orama({
      search: {
        pathMatcher: /^\/docs\//,
        contentSelectors: ['h1', 'h2', '[data-search-meta]'],
      },
    }),
    icon(),
    astroExpressiveCode({
      themes: ['github-light', houston],
      useDarkModeMediaQuery: false,
      themeCssSelector: (theme) => (theme.type === 'dark' ? '.dark' : ':root:not(.dark)'),
      plugins: [pluginLineNumbers()],
      defaultProps: { showLineNumbers: false },
      styleOverrides: {
        borderRadius: '0.75rem',
        borderWidth: '1px',
        borderColor: 'var(--border)',
        codeFontFamily: 'JetBrains Mono, ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
        codeFontSize: '0.875rem',
        codeLineHeight: '1.7',
        codePaddingBlock: '1.25rem',
        codePaddingInline: '1.5rem',
        uiFontFamily: 'Figtree, system-ui, sans-serif',
        focusBorder: '#15803d',
        lineNumbers: {
          foreground: ({ theme }) => (theme.type === 'dark' ? '#9097a3' : '#525964'),
          highlightForeground: ({ theme }) => (theme.type === 'dark' ? '#eef0f9' : '#1a1814'),
        },
        frames: {
          editorActiveTabBackground: ({ theme }) => (theme.type === 'dark' ? '#1a1d24' : '#ffffff'),
          editorActiveTabForeground: ({ theme }) => (theme.type === 'dark' ? '#eef0f9' : '#1a1814'),
          editorActiveTabBorderColor: 'transparent',
          editorActiveTabIndicatorTopColor: '#15803d',
          editorActiveTabIndicatorBottomColor: 'transparent',
          editorTabBarBackground: ({ theme }) => (theme.type === 'dark' ? '#0d1117' : '#f3ede2'),
          editorTabBarBorderColor: 'var(--border)',
          editorTabBarBorderBottomColor: 'var(--border)',
          editorBackground: ({ theme }) => (theme.type === 'dark' ? '#0d1117' : '#ffffff'),
          terminalBackground: ({ theme }) => (theme.type === 'dark' ? '#0d1117' : '#ffffff'),
          terminalTitlebarBackground: ({ theme }) => (theme.type === 'dark' ? '#161b22' : '#f3ede2'),
          terminalTitlebarBorderBottomColor: 'var(--border)',
          terminalTitlebarForeground: ({ theme }) => (theme.type === 'dark' ? '#8b949e' : '#7a7266'),
          frameBoxShadowCssValue: 'none',
          tooltipSuccessBackground: '#10b981',
          tooltipSuccessForeground: '#17191e',
          inlineButtonBackground: 'rgba(21, 128, 61, 0.1)',
          inlineButtonBackgroundHover: 'rgba(21, 128, 61, 0.2)',
          inlineButtonForeground: ({ theme }) => (theme.type === 'dark' ? '#4ade80' : '#15803d'),
          shadowColor: 'transparent',
        },
        textMarkers: {
          markBackground: 'rgba(21, 128, 61, 0.15)',
          markBorderColor: '#15803d',
          insBackground: 'rgba(16, 185, 129, 0.15)',
          insBorderColor: '#10b981',
          delBackground: 'rgba(225, 29, 72, 0.15)',
          delBorderColor: '#e11d48',
        },
      },
    }),
    react({ experimentalReactChildren: true }),
    mdx(),
    sitemap(),
  ],
  vite: {
    plugins: [tailwindcss()],
    build: { target: 'es2022' },
    server: { watch: { ignored: ['**/.wrangler/**'] } },
    resolve: {
      alias: import.meta.env.PROD && {
        'react-dom/server': 'react-dom/server.edge',
      },
      dedupe: ['react', 'react-dom'],
    },
    optimizeDeps: { include: ['react', 'react-dom'] },
  },
  markdown: {
    remarkPlugins: [remarkModifiedTime],
  },
  experimental: {
    fonts: [
      {
        provider: fontProviders.google(),
        name: 'Outfit',
        cssVariable: '--font-outfit',
        weights: ['400', '500', '600', '700', '800'],
        subsets: ['latin'],
        fallbacks: ['ui-sans-serif', 'system-ui', 'sans-serif'],
      },
      {
        provider: fontProviders.google(),
        name: 'Figtree',
        cssVariable: '--font-figtree',
        weights: ['400', '500', '600', '700', '800'],
        subsets: ['latin'],
        fallbacks: ['ui-sans-serif', 'system-ui', 'sans-serif'],
      },
      {
        provider: fontProviders.google(),
        name: 'JetBrains Mono',
        cssVariable: '--font-jetbrains-mono',
        weights: ['400', '500', '700'],
        subsets: ['latin'],
        fallbacks: ['ui-monospace', 'SFMono-Regular', 'Menlo', 'Consolas', 'monospace'],
      },
    ],
  },
  adapter: cloudflare({ imageService: 'compile' }),
});
```

- [ ] **Step 4: Commit**

```bash
git add docs/astro.config.mjs docs/src/env.d.ts docs/houston.theme.json
git commit -m "feat(docs): wire astro.config with green-tinted EC overrides"
```

---

## Task 8: Site config + remark-modified-time + content schema

**Files:**
- Create: `docs/src/data/site.ts`
- Create: `docs/src/remark/remark-modified-time.mjs`
- Create: `docs/src/content.config.ts`
- Create: `docs/src/content/docs/_sections.ts`

- [ ] **Step 1: Copy `remark-modified-time.mjs` verbatim**

```powershell
Copy-Item "C:\Users\mukesh\repos\codewithmukesh\blog\remark-modified-time.mjs" "docs\src\remark\remark-modified-time.mjs"
```

- [ ] **Step 2: Create `docs/src/data/site.ts`**

```ts
const siteConfig = {
  url: 'https://docs.fullstackhero.net',
  title: 'FullStackHero — .NET Starter Kit',
  description:
    'Production-ready modular .NET starter kit. Modular monolith + VSA, multitenancy-first, identity built in.',
  repo: 'https://github.com/fullstackhero/dotnet-starter-kit',
  author: 'Mukesh Murugan',
  ogImage: '/og-default.png',
};

export default siteConfig;
```

- [ ] **Step 3: Create `docs/src/content.config.ts`**

```ts
import { defineCollection, z } from 'astro:content';
import { glob } from 'astro/loaders';

const docs = defineCollection({
  loader: glob({ pattern: '**/*.mdx', base: './src/content/docs' }),
  schema: z.object({
    title: z.string(),
    description: z.string(),
    sidebar: z
      .object({
        label: z.string().optional(),
        order: z.number().default(999),
        hidden: z.boolean().default(false),
      })
      .default({}),
    pageType: z.enum(['guide', 'reference', 'concept', 'recipe']).default('guide'),
    lastUpdated: z.date().optional(),
  }),
});

export const collections = { docs };
```

- [ ] **Step 4: Create `docs/src/content/docs/_sections.ts`**

```ts
export type Section = {
  dir: string;
  label: string;
  order: number;
};

export const sections: Section[] = [
  { dir: 'getting-started', label: 'Getting Started', order: 1 },
  { dir: 'concepts',        label: 'Concepts',        order: 2 },
  { dir: 'modules',         label: 'Modules',         order: 3 },
  { dir: 'recipes',         label: 'Recipes',         order: 4 },
  { dir: 'reference',       label: 'Reference',       order: 5 },
  { dir: 'contributing',    label: 'Contributing',    order: 6 },
];
```

- [ ] **Step 5: Commit**

```bash
git add docs/src/data docs/src/remark docs/src/content.config.ts docs/src/content/docs/_sections.ts
git commit -m "feat(docs): site config, content schema, modified-time remark plugin"
```

---

## Task 9: Theme-init script + BaseLayout

**Files:**
- Create: `docs/src/scripts/theme-init.js`
- Create: `docs/src/layouts/BaseLayout.astro`

- [ ] **Step 1: Create `docs/src/scripts/theme-init.js`**

Inline script that runs before paint to prevent FOUC:

```js
(() => {
  const saved = localStorage.getItem('theme');
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
  const isDark = saved ? saved === 'dark' : prefersDark;
  if (isDark) document.documentElement.classList.add('dark');
})();
```

- [ ] **Step 2: Create `docs/src/layouts/BaseLayout.astro`**

```astro
---
import '../styles/globals.css';
import siteConfig from '../data/site';

interface Props {
  title?: string;
  description?: string;
  image?: string;
  noindex?: boolean;
}

const { title, description, image, noindex } = Astro.props;
const pageTitle = title ? `${title} — ${siteConfig.title}` : siteConfig.title;
const pageDescription = description ?? siteConfig.description;
const pageImage = new URL(image ?? siteConfig.ogImage, Astro.site ?? siteConfig.url).toString();
const canonicalURL = new URL(Astro.url.pathname, Astro.site ?? siteConfig.url).toString();
---
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link rel="canonical" href={canonicalURL} />
    <title>{pageTitle}</title>
    <meta name="description" content={pageDescription} />
    {noindex && <meta name="robots" content="noindex,nofollow" />}

    <meta property="og:type" content="website" />
    <meta property="og:title" content={pageTitle} />
    <meta property="og:description" content={pageDescription} />
    <meta property="og:image" content={pageImage} />
    <meta property="og:url" content={canonicalURL} />
    <meta property="og:site_name" content={siteConfig.title} />

    <meta name="twitter:card" content="summary_large_image" />
    <meta name="twitter:title" content={pageTitle} />
    <meta name="twitter:description" content={pageDescription} />
    <meta name="twitter:image" content={pageImage} />

    <link rel="icon" type="image/svg+xml" href="/favicon.svg" />

    <script is:inline src="/theme-init-placeholder.js"></script>
    <script is:inline define:vars={{}}>
      (() => {
        const saved = localStorage.getItem('theme');
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const isDark = saved ? saved === 'dark' : prefersDark;
        if (isDark) document.documentElement.classList.add('dark');
      })();
    </script>
  </head>
  <body>
    <a href="#main" class="skip-to-content">Skip to content</a>
    <slot />
  </body>
</html>
```

Note: the inline `<script>` block duplicates `theme-init.js` because Astro's `is:inline` doesn't process imports; the file in `src/scripts/` exists as the canonical source kept in sync by hand. (We accept this minor duplication; the inline version is ~100 bytes.)

- [ ] **Step 3: Delete the placeholder `<script src=...>` line**

Remove the line `<script is:inline src="/theme-init-placeholder.js"></script>` from the head — it was a scaffold artifact.

- [ ] **Step 4: Commit**

```bash
git add docs/src/scripts/theme-init.js docs/src/layouts/BaseLayout.astro
git commit -m "feat(docs): BaseLayout with SEO meta, theme init, skip-link"
```

---

## Task 10: ThemeToggle React island (mirror BLOG implementation)

**Files:**
- Create: `docs/src/components/shell/ThemeToggle.tsx`

- [ ] **Step 1: Inspect BLOG ThemeToggle**

```bash
ls C:\Users\mukesh\repos\codewithmukesh\blog\src\components | grep -i theme
```

Identify the file (likely `ThemeToggle.tsx` or similar). Read it.

- [ ] **Step 2: Reproduce it as `docs/src/components/shell/ThemeToggle.tsx`**

Port verbatim, including: View Transitions API flip (`document.startViewTransition`), `localStorage.setItem('theme', ...)`, `classList.toggle('dark')`, ARIA labels, Sun/Moon icons from `lucide-react`. Strip any blog-specific analytics (e.g. PostHog event tracking) if present.

If the blog uses a different filename or framework split, adapt — the goal is functional + visual parity, not exact file naming.

- [ ] **Step 3: Sanity check by adding a temp test page**

Create a throwaway `docs/src/pages/_theme-probe.astro`:

```astro
---
import BaseLayout from '../layouts/BaseLayout.astro';
import ThemeToggle from '../components/shell/ThemeToggle';
---
<BaseLayout title="Theme probe" noindex>
  <main id="main" style="padding: 4rem 2rem">
    <h1>Theme probe</h1>
    <p>Click the toggle. Expect smooth flip between light and dark.</p>
    <ThemeToggle client:load />
  </main>
</BaseLayout>
```

Run `npm run dev` from `docs/`. Visit `http://localhost:4321/_theme-probe/`. Click toggle. Expect smooth View Transitions API cross-fade; html element gets/loses `.dark` class; localStorage `theme` key updates.

- [ ] **Step 4: Delete the probe page**

```bash
rm docs/src/pages/_theme-probe.astro
```

- [ ] **Step 5: Commit**

```bash
git add docs/src/components/shell/ThemeToggle.tsx
git commit -m "feat(docs): ThemeToggle React island with View Transitions"
```

---

## Task 11: Header component (mirror BLOG/src/components/Header.astro)

**Files:**
- Create: `docs/src/components/shell/Header.astro`

- [ ] **Step 1: Read `C:\Users\mukesh\repos\codewithmukesh\blog\src\components\Header.astro`**

Note: classes, structure, breakpoints, sticky behavior, blur, border, height tokens (`--spacing-header` / `--spacing-header-lg`).

- [ ] **Step 2: Reproduce as `docs/src/components/shell/Header.astro` with three changes**

1. **Wordmark**: text "FullStackHero" + small ".docs" suffix in muted (or import a logo SVG if one exists; otherwise plain text wordmark is fine for v1).
2. **Desktop nav items**: replace blog's nav items with exactly:
   - `<a href="/docs/">Docs</a>`
   - `<a href={siteConfig.repo} target="_blank" rel="noopener">GitHub</a>` (with `Github` icon from lucide)
3. **Right-side controls**: keep the search button trigger (wires to SearchModal in Task 19 — for now use a placeholder `<button aria-label="Search">…</button>`), keep the ThemeToggle island, drop any newsletter/login buttons.

Mobile drawer: keep the blog's drawer markup (`#astro-header-drawer` element + `[data-open]` toggling), but its content lists Docs + GitHub only.

- [ ] **Step 3: Verify visually**

Add a temporary `<Header />` import to a probe page or to the eventual `MarketingLayout` (Task 14). Run dev server. Resize from desktop to mobile (< 768px) — confirm drawer animation works (uses `fadeSlideUp` from globals.css), confirm sticky-on-scroll matches blog feel.

- [ ] **Step 4: Commit**

```bash
git add docs/src/components/shell/Header.astro
git commit -m "feat(docs): Header — visual twin of blog with docs nav"
```

---

## Task 12: Footer component (mirror BLOG/src/components/Footer.astro)

**Files:**
- Create: `docs/src/components/shell/Footer.astro`

- [ ] **Step 1: Read blog Footer**

Read `C:\Users\mukesh\repos\codewithmukesh\blog\src\components\Footer.astro`.

- [ ] **Step 2: Reproduce as `docs/src/components/shell/Footer.astro` with adapted links**

Replace blog-specific link groups (Categories, About, etc.) with three columns:

- **Project**: Docs (`/docs/`), Getting Started (`/docs/getting-started/install/`), GitHub (external), Releases (`https://github.com/fullstackhero/dotnet-starter-kit/releases`)
- **Resources**: Architecture (`/docs/concepts/architecture/`), Modules (`/docs/modules/`), Recipes (`/docs/recipes/`), Reference (`/docs/reference/`)
- **More**: codewithmukesh blog (`https://codewithmukesh.com`), License (`https://github.com/fullstackhero/dotnet-starter-kit/blob/main/LICENSE`), Contributing (`/docs/contributing/`)

Keep blog's footer chrome: top border, padding, grid, copyright line at bottom with `© {year} <author>` and a small "made with Astro" tag if the blog has it.

- [ ] **Step 3: Commit**

```bash
git add docs/src/components/shell/Footer.astro
git commit -m "feat(docs): Footer — visual twin with project-relevant links"
```

---

## Task 13: SkipToContent + Marketing 404 + Marketing layout

**Files:**
- Create: `docs/src/components/shell/SkipToContent.astro`
- Create: `docs/src/layouts/MarketingLayout.astro`
- Create: `docs/src/pages/404.astro`

- [ ] **Step 1: Create `docs/src/components/shell/SkipToContent.astro`**

```astro
<a href="#main" class="skip-to-content">Skip to content</a>
```

The `.skip-to-content` class is already styled in `globals.css`.

- [ ] **Step 2: Create `docs/src/layouts/MarketingLayout.astro`**

```astro
---
import BaseLayout from './BaseLayout.astro';
import Header from '../components/shell/Header.astro';
import Footer from '../components/shell/Footer.astro';

interface Props {
  title?: string;
  description?: string;
  image?: string;
}
const { title, description, image } = Astro.props;
---
<BaseLayout {title} {description} {image}>
  <Header />
  <main id="main">
    <slot />
  </main>
  <Footer />
</BaseLayout>
```

- [ ] **Step 3: Create `docs/src/pages/404.astro`**

```astro
---
import MarketingLayout from '../layouts/MarketingLayout.astro';
---
<MarketingLayout title="Not found" description="The page you're looking for doesn't exist.">
  <section class="mx-auto max-w-3xl px-6 py-24 text-center">
    <p class="text-sm font-medium text-primary">404</p>
    <h1 class="mt-4 text-4xl font-bold sm:text-5xl">Page not found</h1>
    <p class="mt-4 text-muted-foreground">
      The page you're looking for doesn't exist or has moved.
    </p>
    <div class="mt-8 flex justify-center gap-3">
      <a href="/" class="rounded-md bg-primary px-4 py-2 text-primary-foreground hover:bg-primary-hover">Home</a>
      <a href="/docs/" class="rounded-md border border-border px-4 py-2 hover:bg-muted">Browse docs</a>
    </div>
  </section>
</MarketingLayout>
```

- [ ] **Step 4: Commit**

```bash
git add docs/src/components/shell/SkipToContent.astro docs/src/layouts/MarketingLayout.astro docs/src/pages/404.astro
git commit -m "feat(docs): MarketingLayout + 404 page"
```

---

## Task 14: Minimal landing page (placeholder Hero)

**Files:**
- Create: `docs/src/pages/index.astro`
- Create: `docs/src/components/landing/Hero.astro` (placeholder)

- [ ] **Step 1: Create placeholder `Hero.astro`**

```astro
---
import siteConfig from '../../data/site';
---
<section class="relative mx-auto max-w-5xl px-6 pt-16 pb-24 text-center sm:pt-24">
  <h1 class="text-4xl font-bold tracking-tight sm:text-6xl">
    The <span class="gradient-text">.NET starter kit</span> that ships
  </h1>
  <p class="mx-auto mt-6 max-w-2xl text-lg text-muted-foreground">
    {siteConfig.description}
  </p>
  <div class="mt-10 flex flex-wrap justify-center gap-3">
    <a href="/docs/getting-started/install/" class="rounded-md bg-primary px-5 py-2.5 font-medium text-primary-foreground hover:bg-primary-hover">
      Get started
    </a>
    <a href={siteConfig.repo} target="_blank" rel="noopener" class="rounded-md border border-border px-5 py-2.5 font-medium hover:bg-muted">
      GitHub
    </a>
  </div>
</section>
```

- [ ] **Step 2: Create `docs/src/pages/index.astro`**

```astro
---
import MarketingLayout from '../layouts/MarketingLayout.astro';
import Hero from '../components/landing/Hero.astro';
---
<MarketingLayout>
  <Hero />
</MarketingLayout>
```

- [ ] **Step 3: Run dev server, verify**

```bash
cd docs && npm run dev
```

Visit `http://localhost:4321/`. Expect: warm paper bg in light mode, header sticky on top, gradient-green headline, two CTAs, footer at bottom. Toggle theme — expect View Transitions flip.

- [ ] **Step 4: Commit**

```bash
git add docs/src/pages/index.astro docs/src/components/landing/Hero.astro
git commit -m "feat(docs): landing page with placeholder hero"
```

---

## Task 15: Seed initial MDX content (index + per-section index)

**Files:**
- Create: `docs/src/content/docs/index.mdx`
- Create: `docs/src/content/docs/getting-started/index.mdx`
- Create: `docs/src/content/docs/getting-started/install.mdx`
- Create: `docs/src/content/docs/concepts/index.mdx`
- Create: `docs/src/content/docs/modules/index.mdx`
- Create: `docs/src/content/docs/recipes/index.mdx`
- Create: `docs/src/content/docs/reference/index.mdx`
- Create: `docs/src/content/docs/contributing/index.mdx`

- [ ] **Step 1: `docs/src/content/docs/index.mdx`**

```mdx
---
title: Documentation
description: Build production-ready .NET apps with the FullStackHero starter kit.
sidebar:
  label: Overview
  order: 1
pageType: guide
---

# Welcome

FullStackHero is a production-ready, modular .NET starter kit. Use these docs to install it, understand the architecture, and ship your own app on top.

## Where to start

- **[Install](/docs/getting-started/install/)** — get a project running locally in under a minute.
- **[Concepts](/docs/concepts/)** — modular monolith, vertical slice architecture, multitenancy, eventing.
- **[Modules](/docs/modules/)** — Identity, Multitenancy, Auditing.
- **[Recipes](/docs/recipes/)** — task-oriented how-tos.
- **[Reference](/docs/reference/)** — config keys, permissions, exceptions.
```

- [ ] **Step 2: `docs/src/content/docs/getting-started/index.mdx`**

```mdx
---
title: Getting Started
description: Install the kit, run it locally, and understand the project layout.
sidebar:
  order: 1
pageType: guide
---

# Getting Started

- [Install](/docs/getting-started/install/)
```

- [ ] **Step 3: `docs/src/content/docs/getting-started/install.mdx`**

```mdx
---
title: Install
description: Clone, build, and run FullStackHero locally.
sidebar:
  order: 1
pageType: guide
---

# Install

## Prerequisites

- .NET 10 SDK
- PostgreSQL 15+ (or use the Aspire AppHost which spins one up)
- Node 20+ (for the admin and dashboard apps)

## Clone

```bash
git clone https://github.com/fullstackhero/dotnet-starter-kit.git
cd dotnet-starter-kit
```

## Build

```bash
dotnet build src/FSH.Starter.slnx
```

## Run

```bash
dotnet run --project src/Host/FSH.Starter.AppHost
```

Open the Aspire dashboard to see the API, database, and frontend apps starting up.
```

- [ ] **Step 4: Stub `index.mdx` for the remaining four sections**

For each of `concepts`, `modules`, `recipes`, `reference`, `contributing`, create `index.mdx`:

```mdx
---
title: <Section Label>
description: <One-line section description>
sidebar:
  order: 1
pageType: guide
---

# <Section Label>

Coming soon.
```

Use the section labels from `_sections.ts`.

- [ ] **Step 5: Commit**

```bash
git add docs/src/content/docs
git commit -m "feat(docs): seed initial MDX content (overview + per-section stubs)"
```

---

## Task 16: Sidebar tree builder helper

**Files:**
- Create: `docs/src/helpers/sidebar.ts`

- [ ] **Step 1: Write the helper**

```ts
import { getCollection, type CollectionEntry } from 'astro:content';
import { sections, type Section } from '../content/docs/_sections';

export type SidebarPage = {
  title: string;
  href: string;
  order: number;
  hidden: boolean;
};

export type SidebarSection = {
  dir: string;
  label: string;
  order: number;
  indexHref: string | null;
  pages: SidebarPage[];
};

function pathFromSlug(slug: string): string {
  // 'getting-started/install' -> '/docs/getting-started/install/'
  // 'index'                   -> '/docs/'
  // 'modules/index'           -> '/docs/modules/'
  if (slug === 'index') return '/docs/';
  const trimmed = slug.replace(/\/index$/, '');
  return `/docs/${trimmed}/`;
}

export async function buildSidebar(): Promise<SidebarSection[]> {
  const all = await getCollection('docs');
  const bySection = new Map<string, CollectionEntry<'docs'>[]>();

  for (const entry of all) {
    if (entry.id === 'index.mdx') continue;
    const [dir] = entry.id.split('/');
    if (!bySection.has(dir)) bySection.set(dir, []);
    bySection.get(dir)!.push(entry);
  }

  const result: SidebarSection[] = sections.map((sec: Section) => {
    const entries = bySection.get(sec.dir) ?? [];
    const indexEntry = entries.find((e) => e.id === `${sec.dir}/index.mdx`);
    const pages = entries
      .filter((e) => e.id !== `${sec.dir}/index.mdx`)
      .filter((e) => !e.data.sidebar.hidden)
      .map<SidebarPage>((e) => {
        const slug = e.id.replace(/\.mdx$/, '');
        return {
          title: e.data.sidebar.label ?? e.data.title,
          href: pathFromSlug(slug),
          order: e.data.sidebar.order,
          hidden: e.data.sidebar.hidden,
        };
      })
      .sort((a, b) => a.order - b.order || a.title.localeCompare(b.title));

    return {
      dir: sec.dir,
      label: sec.label,
      order: sec.order,
      indexHref: indexEntry ? pathFromSlug(`${sec.dir}/index`) : null,
      pages,
    };
  });

  return result.sort((a, b) => a.order - b.order);
}

export function isActivePath(currentPath: string, href: string): boolean {
  const normalize = (p: string) => (p.endsWith('/') ? p : `${p}/`);
  return normalize(currentPath) === normalize(href);
}
```

- [ ] **Step 2: Commit**

```bash
git add docs/src/helpers/sidebar.ts
git commit -m "feat(docs): sidebar tree builder derived from content collection"
```

---

## Task 17: Sidebar.astro

**Files:**
- Create: `docs/src/components/docs/Sidebar.astro`

- [ ] **Step 1: Write the component**

```astro
---
import { buildSidebar, isActivePath } from '../../helpers/sidebar';

const sidebar = await buildSidebar();
const currentPath = Astro.url.pathname;
---
<nav aria-label="Docs sidebar" class="text-sm">
  <ul class="space-y-6">
    {sidebar.map((section) => (
      <li>
        <div class="mb-2 px-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          {section.indexHref ? (
            <a href={section.indexHref} class="hover:text-foreground">{section.label}</a>
          ) : (
            section.label
          )}
        </div>
        {section.pages.length > 0 && (
          <ul class="space-y-0.5">
            {section.pages.map((page) => {
              const active = isActivePath(currentPath, page.href);
              return (
                <li>
                  <a
                    href={page.href}
                    class:list={[
                      'block rounded-md px-3 py-1.5 transition-colors',
                      active
                        ? 'bg-primary/10 font-medium text-primary'
                        : 'text-muted-foreground hover:bg-muted hover:text-foreground',
                    ]}
                    aria-current={active ? 'page' : undefined}
                  >
                    {page.title}
                  </a>
                </li>
              );
            })}
          </ul>
        )}
      </li>
    ))}
  </ul>
</nav>
```

- [ ] **Step 2: Commit**

```bash
git add docs/src/components/docs/Sidebar.astro
git commit -m "feat(docs): Sidebar derived from content collection"
```

---

## Task 18: TableOfContents, PageHeader, Breadcrumbs, PrevNext

**Files:**
- Create: `docs/src/components/docs/TableOfContents.astro`
- Create: `docs/src/components/docs/PageHeader.astro`
- Create: `docs/src/components/docs/Breadcrumbs.astro`
- Create: `docs/src/components/docs/PrevNext.astro`

- [ ] **Step 1: `TableOfContents.astro`**

```astro
---
import type { MarkdownHeading } from 'astro';

interface Props {
  headings: MarkdownHeading[];
}

const { headings } = Astro.props;
const items = headings.filter((h) => h.depth === 2 || h.depth === 3);
---
{items.length > 0 && (
  <nav aria-label="On this page" class="text-sm">
    <p class="mb-3 text-xs font-semibold uppercase tracking-wider text-muted-foreground">On this page</p>
    <ul class="space-y-1.5">
      {items.map((h) => (
        <li class:list={[h.depth === 3 && 'ml-4']}>
          <a href={`#${h.slug}`} class="block text-muted-foreground transition-colors hover:text-foreground">
            {h.text}
          </a>
        </li>
      ))}
    </ul>
  </nav>
)}
```

- [ ] **Step 2: `PageHeader.astro`**

```astro
---
interface Props {
  title: string;
  description: string;
  pageType: 'guide' | 'reference' | 'concept' | 'recipe';
  lastUpdated?: Date;
}

const { title, description, pageType, lastUpdated } = Astro.props;
const typeLabel = {
  guide: 'Guide',
  reference: 'Reference',
  concept: 'Concept',
  recipe: 'Recipe',
}[pageType];
---
<header class="mb-10 border-b border-border pb-6">
  <p class="text-xs font-medium uppercase tracking-wider text-primary">{typeLabel}</p>
  <h1 class="mt-2 text-4xl font-bold tracking-tight">{title}</h1>
  <p class="mt-3 text-lg text-muted-foreground">{description}</p>
  {lastUpdated && (
    <p class="mt-4 text-xs text-muted-foreground">
      Last updated <time datetime={lastUpdated.toISOString()}>{lastUpdated.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })}</time>
    </p>
  )}
</header>
```

- [ ] **Step 3: `Breadcrumbs.astro`**

```astro
---
import { sections } from '../../content/docs/_sections';

interface Props {
  slug: string;
  title: string;
}
const { slug, title } = Astro.props;

const parts = slug.split('/').filter(Boolean);
const items: Array<{ label: string; href: string | null }> = [{ label: 'Docs', href: '/docs/' }];

if (parts.length > 0 && parts[0] !== 'index') {
  const section = sections.find((s) => s.dir === parts[0]);
  if (section) items.push({ label: section.label, href: `/docs/${section.dir}/` });
}

items.push({ label: title, href: null });
---
<nav aria-label="Breadcrumb" class="mb-6 text-sm">
  <ol class="flex flex-wrap items-center gap-1.5 text-muted-foreground">
    {items.map((item, i) => (
      <li class="flex items-center gap-1.5">
        {i > 0 && <span aria-hidden="true">/</span>}
        {item.href ? <a href={item.href} class="hover:text-foreground">{item.label}</a> : <span class="text-foreground">{item.label}</span>}
      </li>
    ))}
  </ol>
</nav>
```

- [ ] **Step 4: `PrevNext.astro`**

```astro
---
import { buildSidebar } from '../../helpers/sidebar';

interface Props {
  currentHref: string;
}
const { currentHref } = Astro.props;
const sidebar = await buildSidebar();

const flat = sidebar.flatMap((s) => s.pages);
const idx = flat.findIndex((p) => p.href === currentHref);
const prev = idx > 0 ? flat[idx - 1] : null;
const next = idx >= 0 && idx < flat.length - 1 ? flat[idx + 1] : null;
---
{(prev || next) && (
  <nav aria-label="Previous and next page" class="mt-16 grid gap-4 border-t border-border pt-8 sm:grid-cols-2">
    {prev ? (
      <a href={prev.href} class="group rounded-lg border border-border p-4 transition-colors hover:bg-muted">
        <span class="block text-xs uppercase tracking-wider text-muted-foreground">Previous</span>
        <span class="mt-1 block font-medium group-hover:text-primary">{prev.title}</span>
      </a>
    ) : <span />}
    {next ? (
      <a href={next.href} class="group rounded-lg border border-border p-4 text-right transition-colors hover:bg-muted sm:col-start-2">
        <span class="block text-xs uppercase tracking-wider text-muted-foreground">Next</span>
        <span class="mt-1 block font-medium group-hover:text-primary">{next.title}</span>
      </a>
    ) : <span />}
  </nav>
)}
```

- [ ] **Step 5: Commit**

```bash
git add docs/src/components/docs/TableOfContents.astro docs/src/components/docs/PageHeader.astro docs/src/components/docs/Breadcrumbs.astro docs/src/components/docs/PrevNext.astro
git commit -m "feat(docs): TOC, PageHeader, Breadcrumbs, PrevNext components"
```

---

## Task 19: MDX component map (Callout + CodeGroup + overrides)

**Files:**
- Create: `docs/src/components/docs/Callout.astro`
- Create: `docs/src/components/docs/CodeGroup.astro`
- Create: `docs/src/components/mdx.ts`
- Create: `docs/src/components/docs/AnchorHeading.astro`

- [ ] **Step 1: `Callout.astro`**

```astro
---
interface Props {
  type?: 'note' | 'warning' | 'tip' | 'danger';
  title?: string;
}
const { type = 'note', title } = Astro.props;

const styles = {
  note:    { wrap: 'bg-muted/40 border-border',                     label: 'text-info' },
  tip:     { wrap: 'bg-success/10 border-success/40',               label: 'text-success' },
  warning: { wrap: 'bg-warning/10 border-warning/40',               label: 'text-warning-foreground' },
  danger:  { wrap: 'bg-destructive/10 border-destructive/40',       label: 'text-destructive' },
}[type];
---
<aside class:list={['my-6 rounded-lg border-l-4 border p-4', styles.wrap]} role="note">
  {title && <p class:list={['mb-1 text-sm font-semibold', styles.label]}>{title}</p>}
  <div class="prose-callout text-sm"><slot /></div>
</aside>
```

- [ ] **Step 2: `CodeGroup.astro`**

A simple tabbed wrapper. Children are expected to be Expressive Code blocks with a `title` attribute.

```astro
---
interface Props {
  labels: string[];
}
const { labels } = Astro.props;
const id = `cg-${Math.random().toString(36).slice(2, 9)}`;
---
<div class="my-6 overflow-hidden rounded-lg border border-border">
  <div role="tablist" class="flex border-b border-border bg-muted/40">
    {labels.map((label, i) => (
      <button
        type="button"
        role="tab"
        data-cg-tab={i}
        data-cg-id={id}
        aria-selected={i === 0 ? 'true' : 'false'}
        class="px-4 py-2 text-sm text-muted-foreground aria-selected:bg-card aria-selected:text-foreground"
      >{label}</button>
    ))}
  </div>
  <div data-cg-panels={id}>
    <slot />
  </div>
</div>

<script is:inline define:vars={{ id }}>
  (() => {
    const tabs = document.querySelectorAll(`[data-cg-id="${id}"]`);
    const panelsRoot = document.querySelector(`[data-cg-panels="${id}"]`);
    const panels = panelsRoot ? Array.from(panelsRoot.children) : [];
    panels.forEach((p, i) => { p.style.display = i === 0 ? '' : 'none'; });
    tabs.forEach((tab) => {
      tab.addEventListener('click', () => {
        const target = Number(tab.getAttribute('data-cg-tab'));
        tabs.forEach((t) => t.setAttribute('aria-selected', t === tab ? 'true' : 'false'));
        panels.forEach((p, i) => { p.style.display = i === target ? '' : 'none'; });
      });
    });
  })();
</script>
```

- [ ] **Step 3: `AnchorHeading.astro`** (used for `<h2>`/`<h3>` MDX override)

```astro
---
interface Props {
  as: 'h2' | 'h3';
  id?: string;
}
const { as: Tag, id } = Astro.props;
---
<Tag id={id} class="group scroll-mt-24">
  <slot />
  {id && (
    <a href={`#${id}`} class="ml-2 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100" aria-label="Link to this section">#</a>
  )}
</Tag>
```

- [ ] **Step 4: `mdx.ts`**

```ts
import Callout from './docs/Callout.astro';
import CodeGroup from './docs/CodeGroup.astro';
import AnchorHeading from './docs/AnchorHeading.astro';

export const mdxComponents = {
  Callout,
  CodeGroup,
  // Note: Astro injects heading slugs automatically into MarkdownHeadings;
  // AnchorHeading is exposed for explicit use inside MDX if authors want
  // an anchor decoration beyond the default Astro rendering.
  AnchorHeading,
};
```

- [ ] **Step 5: Commit**

```bash
git add docs/src/components/docs docs/src/components/mdx.ts
git commit -m "feat(docs): MDX component map (Callout, CodeGroup, AnchorHeading)"
```

---

## Task 20: DocsLayout + dynamic route

**Files:**
- Create: `docs/src/layouts/DocsLayout.astro`
- Create: `docs/src/pages/docs/[...slug].astro`

- [ ] **Step 1: `DocsLayout.astro`**

```astro
---
import BaseLayout from './BaseLayout.astro';
import Header from '../components/shell/Header.astro';
import Footer from '../components/shell/Footer.astro';
import Sidebar from '../components/docs/Sidebar.astro';
import TableOfContents from '../components/docs/TableOfContents.astro';
import type { MarkdownHeading } from 'astro';

interface Props {
  title: string;
  description: string;
  headings: MarkdownHeading[];
  image?: string;
}

const { title, description, headings, image } = Astro.props;
---
<BaseLayout {title} {description} {image}>
  <Header />
  <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
    <div class="lg:grid lg:grid-cols-[16rem_minmax(0,1fr)_14rem] lg:gap-10 xl:gap-12">
      <aside class="hidden lg:sticky lg:top-20 lg:block lg:max-h-[calc(100vh-6rem)] lg:overflow-y-auto lg:py-8">
        <Sidebar />
      </aside>
      <main id="main" class="min-w-0 py-8 lg:py-12">
        <slot />
      </main>
      <aside class="hidden xl:sticky xl:top-20 xl:block xl:max-h-[calc(100vh-6rem)] xl:overflow-y-auto xl:py-8">
        <TableOfContents {headings} />
      </aside>
    </div>
  </div>
  <Footer />
</BaseLayout>
```

- [ ] **Step 2: `src/pages/docs/[...slug].astro`**

```astro
---
import { getCollection, render, type CollectionEntry } from 'astro:content';
import DocsLayout from '../../layouts/DocsLayout.astro';
import PageHeader from '../../components/docs/PageHeader.astro';
import Breadcrumbs from '../../components/docs/Breadcrumbs.astro';
import PrevNext from '../../components/docs/PrevNext.astro';
import { mdxComponents } from '../../components/mdx';

export async function getStaticPaths() {
  const entries = await getCollection('docs');
  return entries.map((entry) => {
    const slug = entry.id.replace(/\.mdx$/, '').replace(/\/index$/, '');
    return {
      params: { slug: slug === 'index' ? undefined : slug },
      props: { entry },
    };
  });
}

interface Props {
  entry: CollectionEntry<'docs'>;
}

const { entry } = Astro.props;
const { Content, headings } = await render(entry);
const currentHref = Astro.url.pathname.endsWith('/') ? Astro.url.pathname : `${Astro.url.pathname}/`;
const slugForCrumb = entry.id.replace(/\.mdx$/, '').replace(/\/index$/, '');
---
<DocsLayout title={entry.data.title} description={entry.data.description} {headings}>
  <Breadcrumbs slug={slugForCrumb} title={entry.data.title} />
  <PageHeader
    title={entry.data.title}
    description={entry.data.description}
    pageType={entry.data.pageType}
    lastUpdated={entry.data.lastUpdated}
  />
  <article class="prose max-w-none">
    <Content components={mdxComponents} />
  </article>
  <PrevNext currentHref={currentHref} />
</DocsLayout>
```

- [ ] **Step 3: Run dev server, verify routes**

```bash
cd docs && npm run dev
```

Visit:
- `http://localhost:4321/docs/` — should render the overview page with sidebar visible on lg+, no TOC (page has no H2s yet)
- `http://localhost:4321/docs/getting-started/install/` — should render the install page with sidebar highlight on "Getting Started > Install" and TOC visible on xl+ showing "Prerequisites", "Clone", "Build", "Run"
- `http://localhost:4321/docs/concepts/` — should render the "Coming soon" stub

- [ ] **Step 4: Commit**

```bash
git add docs/src/layouts/DocsLayout.astro docs/src/pages/docs/[...slug].astro
git commit -m "feat(docs): DocsLayout + dynamic [...slug] route"
```

---

## Task 21: SearchTrigger + SearchModal (Orama-powered)

**Files:**
- Create: `docs/src/components/docs/SearchTrigger.astro`
- Create: `docs/src/components/docs/SearchModal.tsx`

- [ ] **Step 1: Check Orama plugin API**

Open `https://github.com/oramasearch/plugin-astro` README, or run `npm view @orama/plugin-astro` for the version and read its types. The plugin exposes a built index via `import { db } from '@orama/plugin-astro/client'` (verify exact symbol against installed version; pin the import shape that matches what's in `docs/node_modules/@orama/plugin-astro/`).

- [ ] **Step 2: `SearchModal.tsx`**

```tsx
import { useEffect, useRef, useState } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { Search as SearchIcon, X } from 'lucide-react';

type Hit = { id: string; title: string; section?: string; url: string; snippet: string };

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export default function SearchModal({ open, onOpenChange }: Props) {
  const [q, setQ] = useState('');
  const [hits, setHits] = useState<Hit[]>([]);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) setTimeout(() => inputRef.current?.focus(), 0);
    else { setQ(''); setHits([]); }
  }, [open]);

  useEffect(() => {
    if (!q.trim()) { setHits([]); return; }
    let cancelled = false;
    (async () => {
      try {
        const mod = await import('@orama/plugin-astro/client');
        // The plugin exposes a search function; the exact name depends on version.
        // Likely candidates: `search`, `oramaSearch`, or `searchOrama`. Adapt to installed API.
        // Pseudocode below — replace with the real export.
        const searchFn = (mod as any).search ?? (mod as any).default;
        const result = await searchFn({ term: q, limit: 8 });
        if (!cancelled) {
          setHits(
            (result?.hits ?? []).map((h: any) => ({
              id: h.id,
              title: h.document?.title ?? h.document?.h1 ?? 'Untitled',
              section: h.document?.section,
              url: h.document?.url ?? h.document?.path ?? '#',
              snippet: h.document?.content?.slice(0, 160) ?? '',
            }))
          );
        }
      } catch {
        if (!cancelled) setHits([]);
      }
    })();
    return () => { cancelled = true; };
  }, [q]);

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-modal bg-black/40 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-24 z-modal w-[min(90vw,640px)] -translate-x-1/2 rounded-xl border border-border bg-card shadow-2xl">
          <div className="flex items-center gap-3 border-b border-border px-4 py-3">
            <SearchIcon className="size-4 text-muted-foreground" aria-hidden />
            <input
              ref={inputRef}
              type="text"
              value={q}
              onChange={(e) => setQ(e.target.value)}
              placeholder="Search docs…"
              className="flex-1 bg-transparent text-sm outline-none placeholder:text-muted-foreground"
            />
            <Dialog.Close className="rounded p-1 text-muted-foreground hover:bg-muted" aria-label="Close">
              <X className="size-4" />
            </Dialog.Close>
          </div>
          <ul className="max-h-[60vh] overflow-y-auto p-2">
            {hits.length === 0 && q.trim() && (
              <li className="px-3 py-6 text-center text-sm text-muted-foreground">No results.</li>
            )}
            {hits.map((hit) => (
              <li key={hit.id}>
                <a href={hit.url} className="block rounded-md px-3 py-2 hover:bg-muted" onClick={() => onOpenChange(false)}>
                  <div className="text-sm font-medium">{hit.title}</div>
                  {hit.snippet && <div className="mt-0.5 line-clamp-2 text-xs text-muted-foreground">{hit.snippet}</div>}
                </a>
              </li>
            ))}
          </ul>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
```

> Note: the Orama plugin's client export shape changes across versions. If `(mod as any).search` is not the real export, adapt the call site after reading `node_modules/@orama/plugin-astro/dist/client.d.ts` or the package README. The rest of the component (UI + key shortcuts + state) is independent of the search-API shape.

- [ ] **Step 3: `SearchTrigger.astro`**

```astro
---
// Renders the header search button and mounts the modal as a React island.
---
<button
  type="button"
  id="search-trigger"
  class="inline-flex items-center gap-2 rounded-md border border-border bg-muted/40 px-3 py-1.5 text-sm text-muted-foreground hover:bg-muted"
  aria-label="Search docs"
>
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
  <span class="hidden sm:inline">Search</span>
  <kbd class="ml-2 hidden rounded border border-border bg-card px-1.5 py-0.5 text-[10px] sm:inline">Ctrl K</kbd>
</button>

<div id="search-mount"></div>

<script>
  import { createRoot } from 'react-dom/client';
  import { createElement, useState, useEffect } from 'react';
  import SearchModal from './SearchModal';

  function SearchHost() {
    const [open, setOpen] = useState(false);
    useEffect(() => {
      const btn = document.getElementById('search-trigger');
      const onClick = () => setOpen(true);
      btn?.addEventListener('click', onClick);
      const onKey = (e: KeyboardEvent) => {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
          e.preventDefault();
          setOpen(true);
        }
      };
      document.addEventListener('keydown', onKey);
      return () => {
        btn?.removeEventListener('click', onClick);
        document.removeEventListener('keydown', onKey);
      };
    }, []);
    return createElement(SearchModal, { open, onOpenChange: setOpen });
  }

  const mount = document.getElementById('search-mount');
  if (mount) createRoot(mount).render(createElement(SearchHost));
</script>
```

- [ ] **Step 4: Wire SearchTrigger into Header**

In `docs/src/components/shell/Header.astro`, replace the placeholder search `<button>` from Task 11 with:

```astro
import SearchTrigger from '../docs/SearchTrigger.astro';
…
<SearchTrigger />
```

- [ ] **Step 5: Verify in dev**

Run `npm run dev`. Press `Ctrl+K` — modal opens. Type "install" — expect at least one hit pointing to `/docs/getting-started/install/`. Click hit — modal closes, route navigates.

If the Orama API call shape is wrong, this is the task to fix it. Confirm `node_modules/@orama/plugin-astro/dist/client.{d.ts,js,mjs}` to find the real export and update `SearchModal.tsx`.

- [ ] **Step 6: Commit**

```bash
git add docs/src/components/docs/SearchTrigger.astro docs/src/components/docs/SearchModal.tsx docs/src/components/shell/Header.astro
git commit -m "feat(docs): Orama-powered search modal + Ctrl+K shortcut"
```

---

## Task 22: Marketing polish — Hero (full), FeatureGrid, InstallSnippet, FinalCta

**Files:**
- Modify: `docs/src/components/landing/Hero.astro`
- Create: `docs/src/components/landing/FeatureGrid.astro`
- Create: `docs/src/components/landing/InstallSnippet.astro`
- Create: `docs/src/components/landing/FinalCta.astro`
- Modify: `docs/src/pages/index.astro`

- [ ] **Step 1: Upgrade `Hero.astro` to use `brand-shadow`**

```astro
---
import siteConfig from '../../data/site';
---
<section class="relative mx-auto max-w-5xl px-6 pt-20 pb-28 text-center sm:pt-28">
  <div class="brand-shadow" aria-hidden="true"></div>
  <p class="mx-auto inline-flex items-center gap-2 rounded-full border border-border bg-card/60 px-3 py-1 text-xs font-medium text-muted-foreground">
    <span class="size-1.5 rounded-full bg-success pulse-dot"></span>
    v1 in progress
  </p>
  <h1 class="mt-6 text-4xl font-bold tracking-tight sm:text-6xl">
    The <span class="gradient-text">.NET starter kit</span> that ships.
  </h1>
  <p class="mx-auto mt-6 max-w-2xl text-lg text-muted-foreground">
    {siteConfig.description}
  </p>
  <div class="mt-10 flex flex-wrap justify-center gap-3">
    <a href="/docs/getting-started/install/" class="rounded-md bg-primary px-5 py-2.5 font-medium text-primary-foreground hover:bg-primary-hover">
      Get started
    </a>
    <a href={siteConfig.repo} target="_blank" rel="noopener" class="rounded-md border border-border px-5 py-2.5 font-medium hover:bg-muted">
      Star on GitHub
    </a>
  </div>
</section>
```

- [ ] **Step 2: `FeatureGrid.astro`** (uses `magnetic-shimmer` from globals.css)

```astro
---
const features = [
  { icon: 'lucide:layers',         title: 'Modular Monolith',     body: 'Bounded contexts as modules — explicit Contracts boundaries, swap out later if you really need to.' },
  { icon: 'lucide:slice',          title: 'Vertical Slice',       body: 'Each feature is one folder: endpoint, handler, validator. No layered ceremony.' },
  { icon: 'lucide:building-2',     title: 'Multitenant by default', body: 'Tenant isolation is the default. Opt out via IGlobalEntity when you actually need cross-tenant data.' },
  { icon: 'lucide:shield',         title: 'Identity built in',    body: 'JWT bearer + ASP.NET Identity + permission-gated endpoints. No glue code.' },
  { icon: 'lucide:activity',       title: 'Observability',        body: 'Serilog + OpenTelemetry + health checks wired up out of the box.' },
  { icon: 'lucide:rocket',         title: 'Deploy anywhere',      body: 'Aspire AppHost locally; SDK container publish or docker-compose for production.' },
];
import { Icon } from 'astro-icon/components';
---
<section class="mx-auto max-w-6xl px-6 py-20">
  <div class="mx-auto max-w-3xl text-center">
    <h2 class="text-3xl font-bold sm:text-4xl">Built to ship, not to demo</h2>
    <p class="mt-4 text-muted-foreground">Every piece is here because production .NET apps need it. Nothing is here just for show.</p>
  </div>
  <div class="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
    {features.map((f) => (
      <div data-magnetic-card class="relative overflow-hidden rounded-xl border border-border bg-card p-6 transition-colors hover:border-border-strong">
        <Icon name={f.icon} class="size-6 text-primary" />
        <h3 class="mt-4 font-semibold">{f.title}</h3>
        <p class="mt-2 text-sm text-muted-foreground">{f.body}</p>
        <div class="magnetic-shimmer" aria-hidden="true"></div>
      </div>
    ))}
  </div>
</section>
```

- [ ] **Step 3: `InstallSnippet.astro`**

Astro `.astro` files don't accept Markdown code fences. For v1 we render a plain styled `<pre><code>` block (single-language snippet, no copy button needed). Upgrade to Expressive Code rendering later if desired.

```astro
---
---
<section class="mx-auto max-w-3xl px-6 py-16">
  <div class="text-center">
    <h2 class="text-2xl font-bold sm:text-3xl">Get started in one command</h2>
    <p class="mt-3 text-muted-foreground">Clone, build, run. Aspire handles the rest.</p>
  </div>
  <pre class="mt-8 overflow-x-auto rounded-lg border border-border bg-card p-4 text-sm font-mono"><code>git clone https://github.com/fullstackhero/dotnet-starter-kit.git
cd dotnet-starter-kit
dotnet run --project src/Host/FSH.Starter.AppHost</code></pre>
</section>
```

- [ ] **Step 4: `FinalCta.astro`**

```astro
---
import siteConfig from '../../data/site';
---
<section class="border-t border-border bg-gradient-to-b from-transparent to-muted/30">
  <div class="mx-auto max-w-4xl px-6 py-20 text-center">
    <h2 class="text-3xl font-bold sm:text-4xl">Ready to ship?</h2>
    <p class="mt-4 text-muted-foreground">Skim the install guide, read the architecture concepts, and you're 30 minutes from your first feature.</p>
    <div class="mt-8 flex flex-wrap justify-center gap-3">
      <a href="/docs/getting-started/install/" class="rounded-md bg-primary px-5 py-2.5 font-medium text-primary-foreground hover:bg-primary-hover">Get started</a>
      <a href={siteConfig.repo} target="_blank" rel="noopener" class="rounded-md border border-border px-5 py-2.5 font-medium hover:bg-muted">View on GitHub</a>
    </div>
  </div>
</section>
```

- [ ] **Step 5: Compose `index.astro`**

```astro
---
import MarketingLayout from '../layouts/MarketingLayout.astro';
import Hero from '../components/landing/Hero.astro';
import FeatureGrid from '../components/landing/FeatureGrid.astro';
import InstallSnippet from '../components/landing/InstallSnippet.astro';
import FinalCta from '../components/landing/FinalCta.astro';
---
<MarketingLayout>
  <Hero />
  <FeatureGrid />
  <InstallSnippet />
  <FinalCta />
</MarketingLayout>
```

- [ ] **Step 6: Verify and commit**

Run `npm run dev`, visit `/`. Expect: hero with green gradient text + brand-shadow blur in background, 6 feature cards with shimmer-on-hover, install snippet block, final CTA. Toggle theme — all surfaces adapt cleanly.

```bash
git add docs/src/pages/index.astro docs/src/components/landing/
git commit -m "feat(docs): full landing page (Hero, FeatureGrid, InstallSnippet, FinalCta)"
```

---

## Task 23: Favicon, OG image, public assets

**Files:**
- Add: `docs/public/favicon.svg`
- Add: `docs/public/og-default.png`
- Delete: `docs/public/.gitkeep`

- [ ] **Step 1: Create `docs/public/favicon.svg`**

Quick green-square wordmark fallback (replace with a real logo later):

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">
  <rect width="64" height="64" rx="12" fill="#15803d"/>
  <text x="32" y="42" font-family="system-ui, sans-serif" font-size="32" font-weight="800" fill="#fff" text-anchor="middle">FSH</text>
</svg>
```

- [ ] **Step 2: Create `docs/public/og-default.png`**

If you have a real OG image, drop it in. Otherwise, generate a 1200×630 PNG with the wordmark + tagline using any tool; if none is available immediately, copy a placeholder and mark as TODO in `docs/README.md`. The site config already points to `/og-default.png`; missing the file is non-fatal but produces a broken OG preview.

- [ ] **Step 3: Remove placeholder gitkeep**

```bash
rm docs/public/.gitkeep
```

- [ ] **Step 4: Commit**

```bash
git add docs/public
git commit -m "feat(docs): favicon + OG default image"
```

---

## Task 24: Cloudflare Pages config

**Files:**
- Create: `docs/wrangler.toml`

- [ ] **Step 1: Write `wrangler.toml`**

```toml
name = "fullstackhero-docs"
compatibility_date = "2025-09-23"
compatibility_flags = ["nodejs_compat"]
pages_build_output_dir = "dist"
```

- [ ] **Step 2: Verify build**

```bash
cd docs
npm run build
```

Expect: `dist/` produced, no errors. If any Cloudflare-adapter warning appears about React 19 server.edge — the alias in `astro.config.mjs` should already handle it.

- [ ] **Step 3: Local preview**

```bash
npm run preview
```

Visit the preview URL printed in stdout. Smoke-test:
- `/` — landing renders
- `/docs/` — docs home renders, sidebar visible
- `/docs/getting-started/install/` — page + TOC + breadcrumbs + prev/next render
- `/this-does-not-exist` — 404 page renders
- Theme toggle works
- Ctrl+K opens search

- [ ] **Step 4: Commit**

```bash
git add docs/wrangler.toml
git commit -m "feat(docs): Cloudflare Pages config (wrangler.toml)"
```

---

## Task 25: README + final verification

**Files:**
- Create: `docs/README.md`

- [ ] **Step 1: Write `docs/README.md`**

```markdown
# FullStackHero Docs

The documentation site for the FullStackHero .NET starter kit. Built with Astro 5+ and Tailwind 4.

## Local development

```bash
cd docs
npm install
npm run dev
```

The site runs on `http://localhost:4321`. Edit MDX files under `src/content/docs/` and the dev server hot-reloads.

## Build

```bash
npm run build
npm run preview
```

## Deploy

Cloudflare Pages, configured by `wrangler.toml`. Production branch: `main`. Preview deploys on all other branches.

## Content authoring

Add a new doc page by creating an MDX file under `src/content/docs/<section>/`. Frontmatter shape is defined by `src/content.config.ts`. The sidebar tree is derived from file structure; ordering and labels per-section live in `src/content/docs/_sections.ts`.

## Design system

Tokens, prose styles, and code-block themes are forked from `codewithmukesh/blog` (see provenance comments in `src/styles/*.css`). Brand primary is `#15803d` (green-700); the user-facing brand color `#16a34a` lives in `--primary-soft`.
```

- [ ] **Step 2: Final smoke test — build clean from scratch**

```bash
cd docs
rm -rf node_modules dist .astro
npm install
npm run build
```

Expect: clean install (no errors), clean build (no errors or warnings about missing modules). If `astro check` is part of the build, no type errors.

- [ ] **Step 3: Verify no stray purple hexes anywhere**

```bash
grep -rnE "#(4c33d8|6850e8|3f29b3|5942d8|9682fb|a596fb|7659ec)" docs/src docs/astro.config.mjs
```
Expected: no output. Everywhere the blog used purple, we use green.

- [ ] **Step 4: Commit**

```bash
git add docs/README.md
git commit -m "feat(docs): README + final verification"
```

---

## Done criteria

- `cd docs && npm run dev` serves the site at `http://localhost:4321` with no console errors.
- `cd docs && npm run build` produces a `dist/` folder with no build errors.
- Visiting `/`, `/docs/`, `/docs/getting-started/install/`, `/docs/<any-section>/`, and `/some-bad-url` renders correctly.
- Theme toggle flips between light and dark with View Transitions.
- `Ctrl+K` opens search modal; typing "install" returns at least one hit linking to the install page.
- No `#4c33d8` or other blog-purple hexes remain in `docs/src` or `docs/astro.config.mjs`.
- Sidebar shows six sections in the order defined by `_sections.ts`.
- TOC renders on xl+ for any page with `##` headings.
- Prev/Next renders at the bottom of pages with neighbors in the same section flow.

## Out of scope (deferred follow-ups)

- Real OG image (placeholder shipped in Task 23).
- Filling out concept/module/recipe/reference content (skeletal stubs only).
- Reading-time remark plugin.
- PostHog analytics.
- Versioned docs.
- Auto-generated API reference / Scalar embed.
- `motion-reveal.ts` script for `[data-reveal]` IntersectionObserver. The `[data-reveal]` CSS rules are present in `globals.css` (copied verbatim) but no v1 component uses the attribute. Add the script when the first reveal-on-scroll element ships.

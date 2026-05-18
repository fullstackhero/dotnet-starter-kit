# FullStackHero Docs

Documentation site for the FullStackHero .NET starter kit. Built with Astro 6 and Tailwind 4.

## Local development

```bash
cd docs
npm install
npm run dev
```

The site runs on `http://localhost:4321`. Edit MDX files under `src/content/docs/` and the dev server hot-reloads. Note that pagefind search is built only by `npm run build`, so the search modal will report "index not available" during `dev`.

## Build

```bash
npm run build      # astro build && pagefind --site dist
npm run preview    # serves dist/ on a local port
```

## Deploy

Cloudflare Pages, configured by `wrangler.toml`. Production branch: `main`. Build command for the Pages dashboard:

```
npm ci && npm run build
```

Output directory: `dist`.

## Content authoring

Add a new doc page by creating an MDX file under `src/content/docs/<section>/`. Frontmatter shape is defined by `src/content.config.ts`:

```yaml
---
title: Page title
description: One-line description for meta + search snippet
sidebar:
  label: Optional override of title in sidebar
  order: 10                    # lower = higher in section
  hidden: false                # exclude from sidebar but still routable
pageType: guide                # guide | reference | concept | recipe
---
```

The sidebar tree is derived from file structure. Section labels and order live in `src/content/docs/_sections.ts`.

## Design system

Tokens, prose styles, and code-block themes are forked verbatim from `codewithmukesh/blog` (see provenance comments in `src/styles/*.css`). Brand primary is `#15803d` (green-700); the user-facing brand green `#16a34a` lives in `--primary-soft` so it surfaces in accent text, tinted backgrounds, and the gradient mid-stop.

## Search

[Pagefind](https://pagefind.app/). Index is generated as a post-build step over the `dist/` HTML. Only `/docs/*` pages are indexed (scoped via `data-pagefind-body` on the docs `<article>`). Trigger via Ctrl/Cmd+K.

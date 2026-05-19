# Docs site — editorial alignment with `codewithmukesh/blog`

**Date:** 2026-05-19
**Status:** Approved, ready for implementation plan
**Scope:** marketing landing page + global chrome
**Owner:** Mukesh

---

## Background

The FSH docs site at `docs/` was scaffolded by forking tokens and typography from `codewithmukesh/blog`, then rebranding the primary color to green (`#15803d`). Typography, tokens, and the header are direct ports and look the part. However the actual UI surface above the token layer — buttons, cards, callouts, eyebrows, section rhythm, animated affordances — was reinvented inline per section instead of being ported as primitives. The result: typography reads right, but the overall surface feels like a different site.

The owner wants the docs site to feel like a sibling to the blog — same primitives, same eyebrow rhythm, same animation vocabulary, same prose details — while keeping green as the brand color.

## Goals

- Port the blog's `brand/*` primitive system into docs verbatim (color tokens make it green-tinted automatically).
- Adopt the blog's eyebrow numbering pattern across landing sections.
- Wire the magnetic-cards script and animated-IDE pattern that exist as CSS hooks but have no script wiring in docs.
- Align ExpressiveCode theme so code blocks match the editorial tone of the prose.
- Refactor the six landing sections to consume the new primitives.

## Non-goals

- Switching primary color from green back to purple — green is the intentional FSH brand.
- Building a `/styleguide` or `/brand` page (useful but separate scope).
- Polishing the docs reading experience (Sidebar, TOC, MDX prose surface) — separate pass.
- Lifting the blog hero's performative animations (animated terminal, stat-card EQ-tick dance). The docs hero is install-and-CTA-focused, not a marketing performance.
- Adding new landing sections — keeping the existing six.
- Pulling in primitives the docs landing has no use for (TextField, Textarea, Select, Checkbox, Radio, Switch). Reconsider when docs adds forms.

## Already aligned (no work)

These are direct forks from the blog and don't need changes:

- `src/styles/brand-tokens.css` — green gradient retoned, rest identical.
- `src/styles/brand-typography.css` — Outfit / Figtree / JetBrains Mono via Astro Fonts API.
- `src/styles/globals.css` — `section-dots`, `section-dots-subtle`, `magnetic-shimmer` (CSS only), `brand-shadow`, `gradient-text`, `pulse-dot`, `data-reveal`, mobile-nav animations, view-transitions.
- `src/styles/prose.css` — typography + spacing tokens match (one drift to fix: list marker, see below).
- `components/shell/Header.astro` — direct port (sticky, scroll-aware backdrop, liquid hover indicator, mobile drawer). One small fix: GitHub button uses `bg-primary` — replace with `<Button variant="primary">` once primitives land.
- `components/shell/ThemeToggle.astro`, `components/shell/Footer.astro` — direct ports.

## Components to port

New files under `docs/src/components/brand/`, ported verbatim from `codewithmukesh/blog/src/components/brand/`. They reference `var(--primary)` so the green token swap is automatic.

| File | Why we need it |
|---|---|
| `Button.astro` | Ink-on-paper primary (`bg-foreground text-background`). Variants: primary / secondary / ghost / invert / destructive. Sizes: sm / md / lg. Renders `<a>` when `href` given, `<button>` otherwise. Replaces inline `bg-primary` buttons in Hero, FinalCta, Header. |
| `Card.astro` | Replaces inline `rounded-2xl border border-border bg-card p-6` markup in ModuleShowcase and WhatsIncluded. |
| `Callout.astro` | Replaces the docs-local `components/docs/Callout.astro`. |
| `Pill.astro` | Replaces the inline pill in Hero eyebrow and scattered usages. |
| `Kbd.astro` | Replaces inline `<kbd>` in Header search (`Ctrl·K`); future docs prose usage. |

Plus one new file authored in docs (not in blog):

- `components/landing/SectionEyebrow.astro` — small wrapper rendering `01 · The problem` (mono, uppercase, tabular-nums, primary-tinted number). Props: `n` (string), default slot for the label.

## Patterns to port

| Pattern | Source → Destination | Notes |
|---|---|---|
| Magnetic card hover (3D tilt) | `blog/src/scripts/magnetic-cards.ts` → `docs/src/scripts/magnetic-cards.ts` | Wired by adding `import '../scripts/magnetic-cards'` to `BaseLayout.astro`. CSS hook (`magnetic-shimmer`, `[data-magnetic-card]`) already exists in `globals.css`. |
| Animated IDE wrapper | `blog/_Problem.astro` lines 343-495 (the perspective + rotateX/Y hover wrapper, not the whole IDE block) | New `components/landing/AnimatedIde.astro` wrapping a default slot. Applied to the existing VS-Code editor in `CodeFirst.astro`. |
| Section bg alternation | Blog uses `border-y border-border` + `bg-subtle` on alternating sections | Apply: CodeFirst, ModuleShowcase, FinalCta keep subtle bg; Hero, WhatsIncluded, TechStack default. |
| Prose list marker | `blog/src/styles/prose.css:145-161` (SVG star mask) | Replace the green-dot `::marker` block in `docs/src/styles/prose.css`. |
| ExpressiveCode theme | `blog/astro.config.mjs:42-100` | Port the houston theme + frame styling block into `docs/astro.config.mjs`; swap purple focus hex `#4c33d8` → green `#15803d` (and other accent hexes mapped consistently). |

## Section-by-section refactor

After primitives and patterns land, walk through the landing sections.

| Section | Changes |
|---|---|
| `Hero.astro` | Eyebrow → `<Pill>` with pulse-dot. CTAs → `<Button variant="primary">` (now ink-on-paper) + `<Button variant="secondary">`. No eyebrow number — hero is its own thing. |
| `CodeFirst.astro` | Eyebrow → `<SectionEyebrow n="01">Vertical Slice Architecture</SectionEyebrow>`. Wrap editor in `<AnimatedIde>`. |
| `WhatsIncluded.astro` | `<SectionEyebrow n="02">What's included</SectionEyebrow>`. Each grid item uses a lightweight `<Card variant="bare">` (or kept as `<li>` if Card doesn't fit; finalize during implementation). |
| `ModuleShowcase.astro` | `<SectionEyebrow n="03">The three modules</SectionEyebrow>`. Each module card → `<Card data-magnetic-card>` (attribute already in markup; script will pick it up once wired). |
| `TechStack.astro` | `<SectionEyebrow n="04">Under the hood</SectionEyebrow>`. Otherwise unchanged. |
| `FinalCta.astro` | `<SectionEyebrow n="05">Ready to build</SectionEyebrow>`. CTAs → `<Button>`. |

## Header & global chrome

- `Header.astro` GitHub button: swap `bg-primary` inline markup for `<Button variant="primary" size="sm">GitHub</Button>` once primitives land. This is the single drift in the otherwise-faithful Header port.
- Search button + `Kbd` chip in Header: swap inline `<kbd>` for `<Kbd>` primitive once ported.

## Implementation order

Low-risk first; each step independently committable.

1. Port primitives (`Button`, `Card`, `Callout`, `Pill`, `Kbd`) — additions only, zero usage change.
2. Author `SectionEyebrow` and apply to each section.
3. Port magnetic-cards script and verify `ModuleShowcase` cards tilt.
4. Port `AnimatedIde` wrapper and wrap the `CodeFirst` editor.
5. Port ExpressiveCode theme block into `astro.config.mjs`.
6. Refactor Header GitHub button + Hero CTAs + FinalCta CTAs to use `<Button>`.
7. Section background alternation pass.
8. Prose star marker swap.
9. Build + visually walk through Hero → FinalCta in light + dark + mobile.

## Risks / things to verify during implementation

- **Color contrast on ink-on-paper buttons in dark mode.** The blog has tuned tokens for this; verify visually that the swapped primary buttons still pass WCAG AA.
- **Magnetic-cards script + Astro view transitions.** The script needs to re-bind after `astro:after-swap` like the Header script does. Verify on client-side navigation.
- **ExpressiveCode theme port.** Houston theme's purple accents need consistent green substitutions — not just one hex. List every hardcoded `#4c33d8` (or related) in the blog config block and substitute.
- **`<Card variant="bare">` for WhatsIncluded.** Current markup is `<li>` with top border. If Card primitive doesn't have a "bare" variant, leave as `<li>` and don't force it.
- **Existing `components/docs/Callout.astro`** — once the brand Callout lands, decide whether to delete the docs-local one or have it re-export the brand one. Imports across MDX content need updating either way.

## Acceptance

- All six landing sections render through the brand primitives.
- `npx tsc -b` and `npx astro build` are green in `docs/`.
- Visual walk-through in light + dark + mobile shows: ink-on-paper CTAs, numbered eyebrows, tilting module cards, animated CodeFirst editor on hover, themed code blocks, star list markers.
- No regression on the docs reading surface (`/docs/*` pages still render correctly — they only consume `Header`, `Footer`, and prose).

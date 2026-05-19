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
      .default({ order: 999, hidden: false }),
    pageType: z.enum(['guide', 'reference', 'concept', 'recipe']).default('guide'),
    lastUpdated: z.date().optional(),
    /**
     * Per-page SEO overrides. Each field is optional; when omitted the
     * page falls through to the title / description / og-image defaults
     * derived from the frontmatter above and site.ts.
     */
    seo: z
      .object({
        // <title> override. Use this when you want a tighter keyword-
        // optimised <title> different from the on-page H1.
        title: z.string().optional(),
        // <meta name="description"> override. Aim for 140–160 chars.
        description: z.string().optional(),
        // Space- or comma-separated keyword phrases. Not emitted as
        // <meta keywords> (which Google ignores), but consumed by
        // future per-page schema / OG card generation.
        keywords: z.string().optional(),
        // Per-page social share image. Falls through to site.ogImage.
        ogImage: z.string().optional(),
        // Per-page noindex/nofollow flag (e.g. for drafts, legal pages).
        noindex: z.boolean().default(false),
      })
      .default({ noindex: false }),
  }),
});

export const collections = { docs };

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

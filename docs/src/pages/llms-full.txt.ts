import type { APIRoute } from 'astro';
import { getCollection } from 'astro:content';
import siteConfig from '../data/site';

/**
 * /llms-full.txt — the entire documentation corpus concatenated as
 * a single plain-text file for AI ingestion.
 *
 * Why: llms.txt (small summary) is the public-facing index; llms-full.txt
 * is the deep ingest target. AI crawlers that need full context can pull
 * the whole docs site in one fetch instead of crawling every page.
 *
 * Format follows the emerging convention from llmstxt.org:
 *   - A top-level title and project summary.
 *   - Per-page sections separated by `\n\n---\n\n` rules.
 *   - Each section opens with `# {title}`, a blockquote description,
 *     a canonical source URL, and the raw markdown body.
 */
export const GET: APIRoute = async () => {
  const entries = await getCollection('docs');

  // Sort by slug so the output is deterministic across builds.
  const sorted = [...entries].sort((a, b) => a.id.localeCompare(b.id));

  const intro =
    `# fullstackhero — .NET 10 Starter Kit (full documentation)\n\n` +
    `> ${siteConfig.description}\n\n` +
    `Generated from the canonical docs site at ${siteConfig.url}.\n` +
    `Source repository: ${siteConfig.repo}\n` +
    `License: MIT\n` +
    `Pages: ${sorted.length}\n\n`;

  const sections = sorted.map((e) => {
    const slug = e.id.replace(/\.mdx$/, '').replace(/\/index$/, '');
    const url = new URL(`/docs/${slug}/`, siteConfig.url).toString();
    const body = typeof e.body === 'string' ? e.body : '';
    return [
      `# ${e.data.title}`,
      ``,
      `> ${e.data.description}`,
      ``,
      `Source: ${url}`,
      ``,
      body,
    ].join('\n');
  });

  const text = intro + sections.join('\n\n---\n\n') + '\n';

  return new Response(text, {
    headers: {
      'Content-Type': 'text/plain; charset=utf-8',
      'Cache-Control': 'public, max-age=3600',
    },
  });
};

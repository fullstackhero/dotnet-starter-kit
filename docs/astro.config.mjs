import mdx from '@astrojs/mdx';
import sitemap from '@astrojs/sitemap';
import tailwindcss from '@tailwindcss/vite';
import astroExpressiveCode from 'astro-expressive-code';
import icon from 'astro-icon';
import { defineConfig, fontProviders } from 'astro/config';
import { remarkModifiedTime } from './src/remark/remark-modified-time.mjs';
import siteConfig from './src/data/site';

export default defineConfig({
  site: siteConfig.url,
  redirects: {
    // `/docs/` redirects to the introduction so the header's "Docs" tab still
    // lands somewhere sensible. Each section now has its own index page
    // (cards for every child page), so we no longer redirect /docs/getting-started/.
    '/docs': '/docs/getting-started/introduction/',
    '/docs/': '/docs/getting-started/introduction/',
  },
  image: {
    layout: 'constrained',
    responsiveStyles: true,
  },
  prefetch: {
    prefetchAll: false,
    defaultStrategy: 'hover',
  },
  integrations: [
    icon(),
    // Full EC config lives in ec.config.mjs — required to keep the <Code>
    // component working (function-valued options aren't JSON-serializable
    // when inlined here).
    astroExpressiveCode(),
    mdx(),
    sitemap({
      // Per-page priority + changefreq, signalling to crawlers which
      // surfaces matter most. lastmod is set automatically from each
      // file's mtime by @astrojs/sitemap.
      serialize(item) {
        const url = new URL(item.url);
        const path = url.pathname;

        // Homepage gets max priority + weekly cadence (release chip,
        // star counts, etc. tend to refresh).
        if (path === '/' || path === '') {
          item.priority = 1.0;
          item.changefreq = 'weekly';
        }
        // Top-of-funnel install / quick-start pages — high priority,
        // moderate change cadence.
        else if (
          path.startsWith('/docs/getting-started/') ||
          path === '/docs/getting-started/'
        ) {
          item.priority = 0.9;
          item.changefreq = 'monthly';
        }
        // Module + building-block deep-dives — main long-tail surfaces.
        else if (
          path.startsWith('/docs/modules/') ||
          path.startsWith('/docs/building-blocks/') ||
          path.startsWith('/docs/architecture/')
        ) {
          item.priority = 0.8;
          item.changefreq = 'monthly';
        }
        // Guides and recipes — high-quality content, refreshed over time.
        else if (path.startsWith('/docs/guides/')) {
          item.priority = 0.75;
          item.changefreq = 'monthly';
        }
        // Cross-cutting, security, deployment — important reference.
        else if (
          path.startsWith('/docs/cross-cutting-concerns/') ||
          path.startsWith('/docs/security/') ||
          path.startsWith('/docs/deployment/') ||
          path.startsWith('/docs/frontend/') ||
          path.startsWith('/docs/testing/') ||
          path.startsWith('/docs/cli/')
        ) {
          item.priority = 0.7;
          item.changefreq = 'monthly';
        }
        // Changelog gets weekly cadence — search engines love freshness.
        else if (path.startsWith('/docs/changelog/')) {
          item.priority = 0.5;
          item.changefreq = 'weekly';
        }
        // Contributing / meta pages — lower priority.
        else if (path.startsWith('/docs/contributing/')) {
          item.priority = 0.4;
          item.changefreq = 'yearly';
        }
        // Anything else under /docs/ — sensible default.
        else if (path.startsWith('/docs/')) {
          item.priority = 0.6;
          item.changefreq = 'monthly';
        }
        return item;
      },
    }),
  ],
  vite: {
    plugins: [tailwindcss()],
    build: { target: 'es2022' },
    server: { watch: { ignored: ['**/.wrangler/**'] } },
  },
  markdown: {
    remarkPlugins: [remarkModifiedTime],
  },
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
});

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
    '/docs': '/docs/getting-started/introduction/',
    '/docs/': '/docs/getting-started/introduction/',
    '/docs/getting-started': '/docs/getting-started/introduction/',
    '/docs/getting-started/': '/docs/getting-started/introduction/',
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
    sitemap(),
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

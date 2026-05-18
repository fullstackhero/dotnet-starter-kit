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
        codeFontFamily:
          'JetBrains Mono, ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
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

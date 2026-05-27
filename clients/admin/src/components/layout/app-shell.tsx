import { Suspense } from "react";
import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/sidebar";
import { Topbar } from "@/components/layout/topbar";

/**
 * AppShell — three-area layout: sidebar / topbar / canvas. The canvas owns
 * the 4px subgrid texture that telegraphs "this is a console." A subtle
 * radial vignette at the top-right adds depth in dark mode without
 * competing with content.
 */
export function AppShell() {
  return (
    <div className="flex h-screen bg-[var(--color-background)] text-[var(--color-foreground)]">
      {/* Skip link — first focusable element so keyboard/AT users can jump
          past the sidebar + topbar straight to page content. */}
      <a
        href="#main-content"
        className="sr-only z-[100] rounded-md bg-[var(--color-foreground)] px-4 py-2 text-sm font-medium text-[var(--color-background)] focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:outline-none focus:ring-2 focus:ring-[var(--color-ring)]"
      >
        Skip to content
      </a>
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar />
        <main id="main-content" tabIndex={-1} className="relative flex-1 overflow-auto outline-none">
          {/* A soft corner vignette only — the canvas-grid texture used to
              live here too but read as visual noise on dense list surfaces.
              Login + Dashboard hero still apply canvas-mesh locally where
              the editorial reading matters. */}
          <div
            className="pointer-events-none absolute inset-0"
            aria-hidden
            style={{
              background:
                "radial-gradient(60rem 28rem at 92% -4%, oklch(from var(--color-accent-signal) l c h / 0.06), transparent 70%)",
            }}
          />
          {/* Container width is full-bleed by default. If a page needs a
              narrower measure (settings, single-form surfaces), opt into a
              child wrapper there — never widen here.

              Suspense boundary catches lazy-loaded routes during chunk
              fetch — fallback is a tiny mono-caps slug rather than a
              full skeleton, since most chunks are 10–40 KB gzipped and
              resolve in well under a frame. */}
          <div className="relative w-full px-6 py-8 lg:px-10">
            <Suspense
              fallback={
                <div
                  role="status"
                  className="flex min-h-[40vh] items-center justify-center text-sm font-mono uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]"
                >
                  Loading view
                  <span className="caret text-[var(--color-accent-signal)]" aria-hidden />
                </div>
              }
            >
              <Outlet />
            </Suspense>
          </div>
        </main>
      </div>
    </div>
  );
}

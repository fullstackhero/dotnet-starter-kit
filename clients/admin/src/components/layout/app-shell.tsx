import { Suspense } from "react";
import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/sidebar";
import { Topbar } from "@/components/layout/topbar";
import {
  MobileNavProvider,
  MobileNavRoot,
} from "@/components/layout/mobile-nav";
import { InactivityGuard } from "@/components/auth/inactivity-guard";

/**
 * AppShell — three-area layout: sidebar / topbar / main content.
 *
 * Structure mirrors the dashboard shell:
 *   - Desktop sidebar collapses to a 52px icon rail via localStorage state.
 *   - MobileNavRoot mounts the Sheet drawer; MobileNavTrigger in the Topbar
 *     opens it on screens below `md`.
 *   - Suspense boundary in main catches lazy-loaded route chunks.
 *
 * Note: admin does not yet have an ImpersonationBanner — the admin app is
 * the operator surface so it doesn't impersonate itself. If that changes,
 * add a banner component here that reads from admin's AuthContext.
 */
export function AppShell() {
  return (
    <MobileNavProvider>
      {/* Skip-to-content link — first focusable element. */}
      <a
        href="#main-content"
        className="sr-only z-[100] rounded-md bg-[var(--color-foreground)] px-4 py-2 text-sm font-medium text-[var(--color-background)] focus:not-sr-only focus:fixed focus:left-3 focus:top-3 focus:outline-none focus:ring-2 focus:ring-[var(--color-ring)]"
      >
        Skip to content
      </a>

      <div className="flex h-screen flex-col overflow-hidden bg-[var(--color-background)] text-[var(--color-foreground)]">
        <div className="flex min-h-0 flex-1">
          <Sidebar />
          <div className="flex min-w-0 flex-1 flex-col">
            <Topbar />
            <main
              id="main-content"
              tabIndex={-1}
              className="flex-1 overflow-auto p-4 focus:outline-none md:p-6"
            >
              {/* Suspense boundary catches lazy-loaded route chunks.
                  Fallback is kept minimal — most chunks resolve quickly. */}
              <Suspense
                fallback={
                  <div
                    role="status"
                    className="flex min-h-[40vh] items-center justify-center text-sm text-[var(--color-muted-foreground)]"
                  >
                    Loading&hellip;
                  </div>
                }
              >
                <Outlet />
              </Suspense>
            </main>
          </div>
        </div>
      </div>

      {/* Mobile drawer — mounted at root so it portals above the shell. */}
      <MobileNavRoot />

      {/* Inactivity auto-logout — warning modal + countdown, signed-in only. */}
      <InactivityGuard />
    </MobileNavProvider>
  );
}

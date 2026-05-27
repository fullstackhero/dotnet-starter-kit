import { useEffect, useRef, useState } from "react";
import { useLocation } from "react-router-dom";
import { Menu, X } from "lucide-react";
import { SidebarContent } from "@/components/layout/sidebar-content";
import { cn } from "@/lib/cn";

/**
 * MobileNav — hamburger trigger + slide-over drawer for screens below `md`.
 * Closes on route change, on Escape, and on backdrop click. The drawer
 * uses the same SidebarContent as the desktop rail so numbering and
 * active-state styling stay identical.
 */
export function MobileNav() {
  const [open, setOpen] = useState(false);
  const location = useLocation();
  const buttonRef = useRef<HTMLButtonElement>(null);

  // Close on route change. We intentionally listen on pathname only — the
  // search/hash changing shouldn't dismiss the drawer.
  useEffect(() => {
    setOpen(false);
  }, [location.pathname]);

  // Close on Escape; lock body scroll while open.
  useEffect(() => {
    if (!open) return;
    // Capture the trigger node now (it's stable) so the cleanup focuses the
    // right element without reading a possibly-changed ref at teardown.
    const trigger = buttonRef.current;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    document.addEventListener("keydown", onKey);
    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener("keydown", onKey);
      // Return focus to the trigger so the next tap-target is the menu button.
      trigger?.focus();
    };
  }, [open]);

  return (
    <>
      <button
        ref={buttonRef}
        type="button"
        onClick={() => setOpen(true)}
        aria-label="Open navigation"
        aria-expanded={open}
        className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-transparent text-[var(--color-muted-foreground)] transition-colors hover:border-[var(--color-border)] hover:text-[var(--color-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] md:hidden"
      >
        <Menu className="h-4 w-4" />
      </button>

      {/* Drawer + backdrop */}
      <div
        aria-hidden={!open}
        className={cn(
          "fixed inset-0 z-50 md:hidden",
          open ? "pointer-events-auto" : "pointer-events-none",
        )}
      >
        {/* Backdrop — dim + slight blur. canvas-grid sits under it so the
            console texture stays continuous through the dim. */}
        <button
          type="button"
          aria-label="Close navigation"
          onClick={() => setOpen(false)}
          className={cn(
            "absolute inset-0 cursor-default bg-[oklch(0_0_0_/_0.55)] backdrop-blur-sm transition-opacity duration-200",
            open ? "opacity-100" : "opacity-0",
          )}
        />

        {/* Drawer */}
        <aside
          role="dialog"
          aria-modal="true"
          aria-label="Navigation"
          className={cn(
            "absolute inset-y-0 left-0 flex w-72 max-w-[85vw] flex-col border-r border-[var(--color-border)] bg-[var(--color-surface-2)] shadow-2xl transition-transform duration-300 ease-[var(--ease-out-cubic)]",
            open ? "translate-x-0" : "-translate-x-full",
          )}
        >
          {/* Close button positioned in the brand row so it doesn't push content */}
          <button
            type="button"
            onClick={() => setOpen(false)}
            aria-label="Close navigation"
            className="absolute right-3 top-3 z-10 inline-flex h-8 w-8 items-center justify-center rounded-md text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]"
          >
            <X className="h-4 w-4" />
          </button>
          <SidebarContent onNavigate={() => setOpen(false)} />
        </aside>
      </div>
    </>
  );
}

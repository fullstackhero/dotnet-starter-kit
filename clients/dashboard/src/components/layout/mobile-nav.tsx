import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useLocation } from "react-router-dom";
import { Menu } from "lucide-react";
import {
  Sheet,
  SheetContent,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { SidebarNavBody } from "@/components/layout/sidebar";
import { findSectionForPath } from "@/components/layout/nav-data";
import { cn } from "@/lib/cn";

/**
 * Mobile nav drawer.
 *
 * Below `md`, the desktop <Sidebar> is hidden (it's `hidden md:flex`).
 * <MobileNavProvider> mounts a left-edge Sheet that the Topbar's
 * hamburger triggers. The drawer reuses <SidebarNavBody> so there's
 * one source of truth for the nav.
 *
 * Composition:
 *   <MobileNavProvider>      ← provides open state + setter
 *     <MobileNavRoot />       ← renders the Sheet (mount once)
 *     <MobileNavTrigger />    ← the hamburger button (place in topbar)
 *   </MobileNavProvider>
 */

type MobileNavContextValue = {
  open: boolean;
  setOpen: (next: boolean) => void;
};

const MobileNavContext = createContext<MobileNavContextValue | null>(null);

export function useMobileNav() {
  const ctx = useContext(MobileNavContext);
  if (!ctx) throw new Error("useMobileNav must be used within MobileNavProvider");
  return ctx;
}

export function MobileNavProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const value = useMemo(() => ({ open, setOpen }), [open]);
  return (
    <MobileNavContext.Provider value={value}>{children}</MobileNavContext.Provider>
  );
}

/**
 * The Sheet itself. Mount once near the root (inside the provider).
 * Auto-closes on route changes (in case the user uses the back button
 * or any non-NavLink navigation while the drawer is open).
 */
export function MobileNavRoot() {
  const { open, setOpen } = useMobileNav();
  const location = useLocation();

  // Single-select accordion state — local to the drawer so opening it
  // always shows the section that owns the current route.
  const [openSection, setOpenSection] = useState<string | null>(() =>
    findSectionForPath(location.pathname),
  );

  useEffect(() => {
    setOpenSection(findSectionForPath(location.pathname));
  }, [location.pathname]);

  // Belt-and-braces: if the route changes while the drawer is open,
  // close it. NavItemLink also calls onNavigate to close on click.
  useEffect(() => {
    setOpen(false);
    // Intentionally only react to pathname changes, not setOpen identity.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname]);

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetContent side="left" className="flex flex-col p-0">
        {/* Radix Dialog requires a Title for the accessible name; keep it
            visually hidden so the drawer chrome is unchanged. */}
        <DialogTitle className="sr-only">Primary navigation</DialogTitle>
        <DialogDescription className="sr-only">
          Site sections and account links.
        </DialogDescription>
        {/* Brand row — matches Topbar height so the drawer top aligns
            with the rest of the chrome. */}
        <div className="flex h-14 shrink-0 items-center gap-2.5 border-b border-[var(--color-border)] px-4">
          <span
            aria-hidden
            className={cn(
              "brand-mark grid h-7 w-7 place-items-center rounded-md",
              "text-[11px] font-bold tracking-tight text-[var(--color-primary-foreground)]",
              "shadow-[0_1px_0_oklch(1_0_0_/_0.18)_inset,0_4px_14px_-4px_oklch(from_var(--color-primary)_l_c_h_/_0.45)]",
            )}
          >
            F
          </span>
          <span className="font-semibold tracking-tight">fullstackhero</span>
        </div>

        <SidebarNavBody
          collapsed={false}
          openSection={openSection}
          setOpenSection={setOpenSection}
          onNavigate={() => setOpen(false)}
        />

        <div className="border-t border-[var(--color-border)] px-5 py-3">
          <p className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            v0.1 · dashboard
          </p>
        </div>
      </SheetContent>
    </Sheet>
  );
}

/**
 * Hamburger trigger — `md:hidden`. Place in the Topbar (typically as
 * the first child so it sits at the leading edge on small screens).
 */
export function MobileNavTrigger({ className }: { className?: string }) {
  const { setOpen } = useMobileNav();
  const onClick = useCallback(() => setOpen(true), [setOpen]);
  return (
    <button
      type="button"
      aria-label="Open navigation menu"
      onClick={onClick}
      className={cn(
        "grid h-9 w-9 cursor-pointer place-items-center rounded-md md:hidden",
        "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        className,
      )}
    >
      <Menu className="h-4 w-4" aria-hidden />
    </button>
  );
}

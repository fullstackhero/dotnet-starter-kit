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
  Dialog as Sheet,
  SheetContent,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { SidebarNavBody } from "@/components/layout/sidebar";
import { findSectionForPath, sections, filterNavSpec } from "@/components/layout/nav-items";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";
import type { NavSection } from "@/components/layout/nav-items";

/**
 * Mobile nav drawer.
 *
 * Below `md`, the desktop <Sidebar> is hidden. <MobileNavProvider> mounts
 * a left-edge Sheet that the Topbar's hamburger triggers. The drawer reuses
 * <SidebarNavBody> so there is one source of truth for the nav.
 *
 * Composition:
 *   <MobileNavProvider>
 *     <MobileNavRoot />       ← renders the Sheet (mount once at root)
 *     <MobileNavTrigger />    ← the hamburger (place in Topbar)
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
 * The Sheet itself — mount once near the root (inside the provider).
 * Auto-closes on route changes.
 */
export function MobileNavRoot() {
  const { open, setOpen } = useMobileNav();
  const location = useLocation();
  const { user, permissionsHydrated } = useAuth();

  const granted = permissionsHydrated ? (user?.permissions ?? []) : [];
  const visibleSections: NavSection[] = useMemo(
    () =>
      sections
        .map((s) => ({ ...s, items: filterNavSpec(s.items, granted) }))
        .filter((s) => s.items.length > 0),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [granted.join(",")],
  );

  const [openSection, setOpenSection] = useState<string | null>(() =>
    findSectionForPath(location.pathname),
  );

  useEffect(() => {
    setOpenSection(findSectionForPath(location.pathname));
  }, [location.pathname]);

  // Close the drawer on route changes (e.g. browser back/forward).
  useEffect(() => {
    setOpen(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname]);

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetContent side="left" className="flex flex-col p-0">
        {/* Radix Dialog requires a Title for the accessible tree. */}
        <DialogTitle className="sr-only">Primary navigation</DialogTitle>
        <DialogDescription className="sr-only">
          Admin sections and account links.
        </DialogDescription>

        {/* Brand row — matches Topbar height */}
        <div className="flex h-14 shrink-0 items-center gap-2.5 border-b border-[var(--color-border)] px-4">
          <span
            aria-hidden
            className={cn(
              "brand-mark grid h-7 w-7 place-items-center rounded-md shrink-0",
              "text-[11px] font-bold tracking-tight text-[var(--color-primary-foreground)]",
              "shadow-[0_1px_0_oklch(1_0_0_/_0.18)_inset,0_4px_14px_-4px_oklch(from_var(--color-primary)_l_c_h_/_0.45)]",
            )}
          >
            F
          </span>
          <div className="flex flex-col">
            <span className="whitespace-nowrap font-display text-[15px] font-bold leading-none tracking-tight text-[var(--color-foreground)]">
              fullstack<span className="text-[var(--color-primary)]">hero</span>
            </span>
            <span className="mt-0.5 text-[10px] font-semibold uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]">
              Admin
            </span>
          </div>
        </div>

        <SidebarNavBody
          collapsed={false}
          openSection={openSection}
          setOpenSection={setOpenSection}
          visibleSections={visibleSections}
          onNavigate={() => setOpen(false)}
        />

        <div className="border-t border-[var(--color-border)] px-5 py-3">
          <p className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            v0.1 · admin
          </p>
        </div>
      </SheetContent>
    </Sheet>
  );
}

/**
 * Hamburger trigger — `md:hidden`. Place in the Topbar.
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

/**
 * @deprecated Use MobileNavTrigger + MobileNavProvider + MobileNavRoot instead.
 * Kept only for any lingering direct usages of the old <MobileNav />.
 */
export { MobileNavTrigger as MobileNav };

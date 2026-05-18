import { SidebarContent } from "@/components/layout/sidebar-content";

/**
 * Sidebar — desktop-only fixed-width rail. Below `md` the AppShell mounts
 * <MobileNav /> instead, which uses the same <SidebarContent /> in a
 * slide-over drawer.
 */
export function Sidebar() {
  return (
    <aside className="hidden w-64 shrink-0 border-r border-[var(--color-border)] bg-[var(--color-surface-2)] md:flex md:flex-col">
      <SidebarContent />
    </aside>
  );
}

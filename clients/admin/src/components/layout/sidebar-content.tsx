/**
 * sidebar-content.tsx — compatibility shim.
 *
 * The sidebar's nav body now lives in sidebar.tsx as <SidebarNavBody>.
 * This file is kept so any lingering imports (e.g. tests) don't break.
 * Mobile nav uses <SidebarNavBody> directly.
 */
export { SidebarNavBody as SidebarContent } from "@/components/layout/sidebar";

import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/sidebar";
import { Topbar } from "@/components/layout/topbar";
import { ImpersonationBanner } from "@/components/layout/impersonation-banner";
import { SseProvider } from "@/sse/sse-context";
import { CommandPaletteRoot } from "@/components/command-palette/command-palette";

export function AppShell() {
  return (
    <SseProvider>
      <div className="flex h-screen flex-col bg-[var(--color-background)] text-[var(--color-foreground)]">
        <ImpersonationBanner />
        <div className="flex min-h-0 flex-1">
          <Sidebar />
          <div className="flex min-w-0 flex-1 flex-col">
            <Topbar />
            <main className="flex-1 overflow-auto p-6">
              <Outlet />
            </main>
          </div>
        </div>
      </div>
      {/* Mounted inside the router subtree so useNavigate inside the
          palette resolves correctly. */}
      <CommandPaletteRoot />
    </SseProvider>
  );
}

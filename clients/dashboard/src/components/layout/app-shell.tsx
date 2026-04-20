import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/sidebar";
import { Topbar } from "@/components/layout/topbar";
import { SseProvider } from "@/sse/sse-context";

export function AppShell() {
  return (
    <SseProvider>
      <div className="flex h-screen bg-[var(--color-background)] text-[var(--color-foreground)]">
        <Sidebar />
        <div className="flex min-w-0 flex-1 flex-col">
          <Topbar />
          <main className="flex-1 overflow-auto p-6">
            <Outlet />
          </main>
        </div>
      </div>
    </SseProvider>
  );
}

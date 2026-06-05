import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/sidebar";
import { Topbar } from "@/components/layout/topbar";
import { ImpersonationBanner } from "@/components/layout/impersonation-banner";
import { ExpiryBanner } from "@/components/layout/expiry-banner";
import {
  MobileNavProvider,
  MobileNavRoot,
} from "@/components/layout/mobile-nav";
import { SseProvider } from "@/sse/sse-context";
import { RealtimeProvider } from "@/realtime/realtime-context";
import { ChatGlobalNotifier } from "@/components/notifications/chat-global-notifier";
import { CommandPaletteRoot } from "@/components/command-palette/command-palette";
import { InactivityGuard } from "@/components/auth/inactivity-guard";
import { cn } from "@/lib/cn";

export function AppShell() {
  return (
    <SseProvider>
      <RealtimeProvider>
      <MobileNavProvider>
        {/* Skip-to-content link — first focusable element. Visually
            hidden until focused, then it lifts up as a brand chip so
            keyboard users can jump past the chrome on every page. */}
        <a
          href="#main"
          className={cn(
            "sr-only focus:not-sr-only focus:fixed focus:left-3 focus:top-3 focus:z-[100]",
            "focus:rounded-md focus:bg-[var(--color-primary)] focus:px-3 focus:py-1.5",
            "focus:text-sm focus:font-medium focus:text-[var(--color-primary-foreground)]",
            "focus:shadow-[var(--shadow-md)] focus:outline-none focus:ring-2",
            "focus:ring-[var(--color-ring)] focus:ring-offset-2 focus:ring-offset-[var(--color-background)]",
          )}
        >
          Skip to main content
        </a>

        <div className="flex h-screen flex-col overflow-hidden bg-[var(--color-background)] text-[var(--color-foreground)]">
          <ImpersonationBanner />
          <ExpiryBanner />
          <div className="flex min-h-0 flex-1">
            <Sidebar />
            <div className="flex min-w-0 flex-1 flex-col">
              <Topbar />
              <main
                id="main"
                tabIndex={-1}
                className="flex-1 overflow-auto p-4 focus:outline-none md:p-6"
              >
                <Outlet />
              </main>
            </div>
          </div>
        </div>

        {/* Mobile drawer — mounted at root so it can portal above the
            shell. Hidden via Sheet open state; the trigger lives in
            the Topbar. */}
        <MobileNavRoot />
      </MobileNavProvider>
      {/* Background chat notifier — listens to ChatMessageCreated on the
          shared SignalR connection and toasts when the user isn't currently
          on that channel. Mounted inside the router subtree so the route
          predicate (current /chat/:channelId) and navigate() both work. */}
      <ChatGlobalNotifier />

      {/* Mounted inside the router subtree so useNavigate inside the
          palette resolves correctly. */}
      <CommandPaletteRoot />

      {/* Inactivity auto-logout — warning modal + countdown, signed-in only. */}
      <InactivityGuard />
      </RealtimeProvider>
    </SseProvider>
  );
}

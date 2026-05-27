import { Suspense } from "react";
import { RouterProvider } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "sonner";
import { queryClient } from "@/lib/query-client";
import { AuthProvider } from "@/auth/auth-context";
import { RealtimeProvider } from "@/realtime/realtime-context";
import { ThemeProvider } from "@/components/theme/theme-provider";
import { router } from "@/routes";

export function App() {
  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <RealtimeProvider>
            {/* Top-level boundary so the public lazy routes (login, password
                reset, confirm-email) have a Suspense ancestor on cold chunk
                fetch — the protected routes also have AppShell's own. */}
            <Suspense
              fallback={
                <div
                  role="status"
                  aria-label="Loading"
                  className="grid min-h-dvh place-items-center bg-[var(--color-background)]"
                />
              }
            >
              <RouterProvider router={router} />
            </Suspense>
          </RealtimeProvider>
          <Toaster
            position="top-right"
            theme="system"
            closeButton
            toastOptions={{
              className: "font-sans text-sm",
              style: {
                borderRadius: "var(--radius-lg)",
                border: "1px solid var(--color-border-strong)",
                backgroundColor: "var(--color-surface-2)",
                color: "var(--color-foreground)",
                fontFamily: "var(--font-sans)",
              },
            }}
          />
        </AuthProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}

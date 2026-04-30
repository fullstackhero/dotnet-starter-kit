import { RouterProvider } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "sonner";
import { queryClient } from "@/lib/query-client";
import { AuthProvider } from "@/auth/auth-context";
import { ThemeProvider } from "@/components/theme/theme-provider";
import { CommandPaletteProvider } from "@/components/command-palette/command-palette";
import { router } from "@/routes";

export function App() {
  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <CommandPaletteProvider>
            <RouterProvider router={router} />
            {/*
              FSH-native toaster.

              We deliberately don't enable `richColors` — sonner's default palette
              paints the whole toast surface in saturated green/red, which fights
              the dashboard's restrained OKLCH vocabulary. Instead each toast
              shares the same `gradient-border surface-edge` chrome the rest of
              the app uses, with a 2px tone-coded edge accent on the left and a
              tinted icon plate. Type styling lives in `globals.css` under the
              `.fsh-toast` family (one rule per [data-type]).
            */}
            <Toaster
              position="top-right"
              closeButton
              theme="system"
              gap={10}
              toastOptions={{
                duration: 4200,
                classNames: {
                  toast: "fsh-toast",
                  title: "fsh-toast-title",
                  description: "fsh-toast-description",
                  closeButton: "fsh-toast-close",
                  actionButton: "fsh-toast-action",
                  cancelButton: "fsh-toast-cancel",
                },
              }}
            />
          </CommandPaletteProvider>
        </AuthProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}

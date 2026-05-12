import { RouterProvider } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "sonner";
import { queryClient } from "@/lib/query-client";
import { AuthProvider } from "@/auth/auth-context";
import { ThemeProvider, useTheme } from "@/components/theme/theme-provider";
import { CommandPaletteProvider } from "@/components/command-palette/command-palette";
import { router } from "@/routes";

export function App() {
  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <CommandPaletteProvider>
            <RouterProvider router={router} />
            <FshToaster />
          </CommandPaletteProvider>
        </AuthProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}

/**
 * FSH-native toaster — "tone rail" treatment.
 *
 * `richColors` is intentionally off; the colour story lives in a 3px tone-coloured left rail
 * (border-left, never repainted) and a small lowercase mono pill that lines up inline with the
 * title (`ok` / `err` / `warn` / `info` / `note`). The default sonner icon plate is hidden —
 * type is announced typographically. All chrome lives in globals.css under the `.fsh-toast`
 * family; the `closeButton` flag below makes sonner render the hairline X we re-style there.
 *
 * `theme` is sourced from the app's ThemeProvider (light/dark) rather than sonner's "system"
 * default — otherwise the toast follows the OS prefers-color-scheme instead of the in-app
 * theme toggle, so a user on a light OS who switched the dashboard to dark gets a glaring
 * white toast.
 */
function FshToaster() {
  const { resolved } = useTheme();
  return (
    <Toaster
      position="top-right"
      closeButton
      theme={resolved}
      gap={10}
      expand
      visibleToasts={4}
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
  );
}

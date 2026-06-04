import { RouterProvider } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "sonner";
import {
  AlertCircle,
  AlertTriangle,
  CheckCircle2,
  Info,
  Loader2,
} from "lucide-react";
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
 * FSH toaster — calm warm-paper card with per-type tone-tinted icon.
 *
 * Per-type Lucide icon, a tone-tinted disc on the left, a plain
 * `bg-card border border-border` surface that matches the rest of the
 * dos vocabulary. `theme` is sourced from the app's ThemeProvider
 * (light/dark) rather than sonner's "system" default — otherwise the
 * toast follows the OS prefers-color-scheme instead of the in-app
 * theme toggle.
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
      icons={{
        success: <CheckCircle2 className="fsh-toast-glyph" strokeWidth={2.25} />,
        error: <AlertCircle className="fsh-toast-glyph" strokeWidth={2.25} />,
        warning: <AlertTriangle className="fsh-toast-glyph" strokeWidth={2.25} />,
        info: <Info className="fsh-toast-glyph" strokeWidth={2.25} />,
        loading: <Loader2 className="fsh-toast-glyph fsh-toast-glyph-spin" strokeWidth={2.25} />,
      }}
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

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
 * FSH-native toaster — frosted "console card" treatment.
 *
 * Per-type Lucide icon rendered inside a tone-tinted disc (top-left), a
 * frosted gradient-bordered surface that matches the DropdownMenu /
 * Dialog vocabulary, a radial brand-tone glow behind the card, and a
 * refined drain bar tinted with the same tone. All chrome lives in
 * globals.css under the `.fsh-toast` family; the `closeButton` flag
 * makes sonner render the hairline X we re-style there.
 *
 * `theme` is sourced from the app's ThemeProvider (light/dark) rather
 * than sonner's "system" default — otherwise the toast follows the OS
 * prefers-color-scheme instead of the in-app theme toggle.
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

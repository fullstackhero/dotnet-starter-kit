import { Moon, Sun } from "lucide-react";
import { useTheme } from "@/components/theme/theme-provider";
import { cn } from "@/lib/cn";

/**
 * ThemeToggle — flips between light and dark off the currently resolved
 * theme. Renders the *destination* icon (i.e. if you're in dark, you see
 * Sun, because clicking will send you to light) so it reads as a
 * directional affordance rather than a status. The richer mode picker
 * (light / dark / system) lives in the topbar profile menu and the
 * Settings → Appearance page.
 */
export function ThemeToggle({ className }: { className?: string }) {
  const { resolved, setMode } = useTheme();
  const Icon = resolved === "dark" ? Sun : Moon;
  return (
    <button
      type="button"
      onClick={() => setMode(resolved === "dark" ? "light" : "dark")}
      aria-label={resolved === "dark" ? "Switch to light theme" : "Switch to dark theme"}
      className={cn(
        "group relative inline-flex h-8 w-8 items-center justify-center rounded-md border border-transparent text-[var(--color-muted-foreground)] transition-colors hover:border-[var(--color-border)] hover:text-[var(--color-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        className,
      )}
    >
      <Icon className="h-4 w-4" />
    </button>
  );
}

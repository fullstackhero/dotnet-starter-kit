import { Moon, Sun } from "lucide-react";
import { useTheme } from "@/components/theme/theme-provider";
import { cn } from "@/lib/cn";

/**
 * ThemeToggle — switches between light and dark. Renders the *destination*
 * icon (i.e. if you're in dark, you see Sun, because clicking will send you
 * to light) so it reads as a directional affordance rather than a status.
 */
export function ThemeToggle({ className }: { className?: string }) {
  const { theme, toggle } = useTheme();
  const Icon = theme === "dark" ? Sun : Moon;
  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={theme === "dark" ? "Switch to light theme" : "Switch to dark theme"}
      className={cn(
        "group relative inline-flex h-8 w-8 items-center justify-center rounded-md border border-transparent text-[var(--color-muted-foreground)] transition-colors hover:border-[var(--color-border)] hover:text-[var(--color-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        className,
      )}
    >
      <Icon className="h-4 w-4" />
    </button>
  );
}

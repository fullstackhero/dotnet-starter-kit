import { Moon, Sun } from "lucide-react";
import { useTheme } from "@/components/theme/theme-provider";
import { Button } from "@/components/ui/button";
import { FormSection, FormShell } from "@/components/list";
import { cn } from "@/lib/cn";

type Mode = "light" | "dark";

const MODES: { value: Mode; label: string; icon: typeof Sun; blurb: string }[] = [
  { value: "light", label: "Light", icon: Sun, blurb: "Paper-white surfaces, magazine-print mood." },
  { value: "dark", label: "Dark", icon: Moon, blurb: "Console-default. Lower glare for long sessions." },
];

/**
 * AppearanceSettings — theme picker. ThemeProvider only carries a binary
 * light/dark today; a future "Follow system" mode would extend the provider
 * to a tri-state. Persistence is handled by the provider; we just call setTheme.
 */
export function AppearanceSettings() {
  const { theme, setTheme } = useTheme();

  return (
    <FormShell>
      <FormSection
        title="Theme"
        description="Console looks good in both modes — the editorial-terminal language is built around tone-neutral surfaces with a single chartreuse accent that reads identically on either."
      >
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {MODES.map(({ value, label, icon: Icon, blurb }) => {
            const active = theme === value;
            return (
              <button
                key={value}
                type="button"
                onClick={() => setTheme(value)}
                aria-pressed={active}
                className={cn(
                  "group/card flex flex-col items-start gap-2 rounded-md border px-4 py-3 text-left transition-colors",
                  active
                    ? "border-[var(--color-accent-signal)] bg-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.08)]"
                    : "border-[var(--color-border)] hover:bg-[var(--color-muted)]/60",
                )}
              >
                <span className="grid h-8 w-8 place-items-center rounded-md bg-[var(--color-surface-2)] text-[var(--color-foreground)]">
                  <Icon className="h-4 w-4" />
                </span>
                <span className="text-sm font-medium">{label}</span>
                <span className="text-xs text-[var(--color-muted-foreground)]">{blurb}</span>
              </button>
            );
          })}
        </div>
      </FormSection>

      <FormSection
        title="Density"
        description="Coming soon — admin will get a compact-rows toggle for dense tables, similar to the dashboard's density-toggle."
      >
        <Button variant="outline" size="sm" disabled>
          Compact rows · coming soon
        </Button>
      </FormSection>
    </FormShell>
  );
}

import { Moon, Palette, Sun } from "lucide-react";
import { useTranslation } from "react-i18next";
import { useTheme } from "@/components/theme/theme-provider";
import { Button } from "@/components/ui/button";
import { SettingsSection } from "@/components/list";
import { cn } from "@/lib/cn";

type Mode = "light" | "dark";

const MODES: {
  value: Mode;
  labelKey: string;
  icon: typeof Sun;
  blurbKey: string;
}[] = [
  {
    value: "light",
    labelKey: "appearance.theme.light",
    icon: Sun,
    blurbKey: "appearance.theme.lightBlurb",
  },
  {
    value: "dark",
    labelKey: "appearance.theme.dark",
    icon: Moon,
    blurbKey: "appearance.theme.darkBlurb",
  },
];

/**
 * AppearanceSettings — theme picker. ThemeProvider carries a binary
 * light/dark today; a future "Follow system" mode would extend the provider
 * to a tri-state. Persistence is handled by the provider; we just call setTheme.
 */
export function AppearanceSettings() {
  const { t } = useTranslation("settings");
  const { theme, setTheme } = useTheme();

  return (
    <div className="space-y-5 fsh-enter">
      {/* Theme */}
      <SettingsSection
        title={t("appearance.theme.title")}
        icon={Palette}
        description={t("appearance.theme.description")}
      >
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {MODES.map(({ value, labelKey, icon: Icon, blurbKey }) => {
            const active = theme === value;
            return (
              <button
                key={value}
                type="button"
                onClick={() => setTheme(value)}
                aria-pressed={active}
                className={cn(
                  "group/card relative overflow-hidden flex flex-col items-start gap-2 rounded-xl border p-4 text-left",
                  "transition-colors duration-[var(--duration-default)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
                  active
                    ? "border-[var(--color-accent-signal)] bg-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.08)]"
                    : "border-[var(--color-border)] bg-[var(--color-card)] hover:bg-[var(--color-muted)]",
                )}
              >
                <div className="flex w-full items-center justify-between">
                  <span
                    className={cn(
                      "grid h-8 w-8 place-items-center rounded-md",
                      active
                        ? "bg-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.12)] text-[var(--color-accent-signal)]"
                        : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
                    )}
                  >
                    <Icon className="h-4 w-4" />
                  </span>
                  {active && (
                    <span className="text-[10px] font-semibold uppercase tracking-wider text-[var(--color-accent-signal)]">
                      {t("appearance.active")}
                    </span>
                  )}
                </div>
                <span
                  className={cn(
                    "text-sm font-semibold tracking-tight",
                    active && "text-[var(--color-accent-signal)]",
                  )}
                >
                  {t(labelKey)}
                </span>
                <span className="text-xs leading-relaxed text-[var(--color-muted-foreground)]">
                  {t(blurbKey)}
                </span>
              </button>
            );
          })}
        </div>
      </SettingsSection>

      {/* Density — placeholder for a future compact toggle */}
      <SettingsSection
        title={t("appearance.density.title")}
        icon={Palette}
        description={t("appearance.density.description")}
      >
        <Button variant="outline" size="sm" disabled>
          {t("appearance.density.comingSoon")}
        </Button>
      </SettingsSection>
    </div>
  );
}

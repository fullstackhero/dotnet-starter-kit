import { Monitor, Moon, Sun } from "lucide-react";
import { useTheme, type ThemeMode } from "@/components/theme/theme-provider";
import { cn } from "@/lib/cn";

const options: Array<{
  value: ThemeMode;
  label: string;
  Icon: React.ComponentType<{ className?: string }>;
}> = [
  { value: "light", label: "Light", Icon: Sun },
  { value: "system", label: "System", Icon: Monitor },
  { value: "dark", label: "Dark", Icon: Moon },
];

/**
 * Three-segment theme switcher with an animated thumb that slides
 * between segments. The thumb position is driven by a CSS attribute
 * selector on `data-mode`, so the animation runs without React having
 * to coordinate any stateful transition.
 */
export function ThemeToggle() {
  const { mode, setMode } = useTheme();
  return (
    <div
      role="radiogroup"
      aria-label="Theme"
      data-mode={mode}
      className="seg-toggle gradient-border inline-flex h-8 items-center rounded-full bg-[var(--color-surface-2)] p-0.5"
    >
      <span className="seg-thumb" aria-hidden />
      {options.map(({ value, label, Icon }) => {
        const active = mode === value;
        return (
          <button
            key={value}
            type="button"
            role="radio"
            aria-checked={active}
            aria-label={label}
            title={label}
            onClick={() => setMode(value)}
            className={cn(
              "relative z-10 inline-flex h-7 w-7 cursor-pointer items-center justify-center rounded-full",
              "transition-colors duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              active
                ? "text-[var(--color-primary-foreground)]"
                : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
            )}
          >
            <Icon className="h-3.5 w-3.5" />
          </button>
        );
      })}
    </div>
  );
}

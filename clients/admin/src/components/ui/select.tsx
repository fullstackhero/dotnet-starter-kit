import { Check, ChevronDown } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

export type SelectOption<T extends string = string> = {
  value: T;
  label: string;
  hint?: string;
};

export type SelectProps<T extends string = string> = {
  /** Currently-selected value. Pass "" to indicate the "all / empty" state. */
  value: T | "";
  onChange: (value: T | "") => void;
  options: SelectOption<T>[];
  /**
   * When provided, a leading option with value="" is rendered with this label
   * (e.g. "All statuses", "Any role"). When the user picks it, onChange is
   * called with "".
   */
  placeholder?: string;
  /** Optional visible label rendered before the trigger button. */
  label?: string;
  className?: string;
  disabled?: boolean;
  /** Minimum width applied to both trigger and content. Defaults to 10rem. */
  minWidth?: string;
};

/**
 * Select — a modern single-select dropdown built on the existing Radix
 * DropdownMenu primitive. Replaces the native <select> filter controls
 * throughout the admin app.
 *
 * Design goals:
 *   • Trigger button matches the outline Button visual language (border,
 *     rounded-lg, h-9, font-sans text-sm).
 *   • Selected option gets a rose brand check mark and tinted background.
 *   • Content panel shares the frosted/gradient-border vocabulary already
 *     used by DropdownMenuContent (no new visual primitives).
 *   • Fully keyboard-accessible via Radix roving focus.
 */
export function Select<T extends string = string>({
  value,
  onChange,
  options,
  placeholder,
  label,
  className,
  disabled = false,
  minWidth = "10rem",
}: SelectProps<T>) {
  const allOptions: SelectOption<string>[] = placeholder
    ? [{ value: "", label: placeholder }, ...options]
    : [...options];

  const current =
    allOptions.find((o) => o.value === value) ??
    (placeholder ? { value: "", label: placeholder } : allOptions[0]);

  const displayLabel = current?.label ?? placeholder ?? "Select…";
  const hasSelection = value !== "";

  return (
    <div className={cn("flex items-center gap-2", className)}>
      {label && (
        <span className="shrink-0 font-mono text-[0.6875rem] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
          {label}
        </span>
      )}

      <DropdownMenu>
        <DropdownMenuTrigger
          disabled={disabled}
          style={{ minWidth }}
          className={cn(
            // Base shape — matches outline Button
            "group inline-flex h-9 items-center justify-between gap-2 rounded-lg border border-[var(--color-input)]",
            "bg-[var(--color-card)] px-3 py-2 text-sm font-sans shadow-xs",
            // Colour
            hasSelection
              ? "text-[var(--color-foreground)]"
              : "text-[var(--color-muted-foreground)]",
            // Interactions
            "transition-[border-color,box-shadow,background-color] duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
            "hover:border-[var(--color-border-strong)] hover:bg-[var(--color-accent)]",
            "focus-visible:outline-none focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
            "data-[state=open]:border-[var(--color-ring)] data-[state=open]:ring-[3px] data-[state=open]:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
            "disabled:cursor-not-allowed disabled:opacity-50",
          )}
        >
          <span className="truncate">{displayLabel}</span>
          <ChevronDown
            aria-hidden
            className={cn(
              "h-4 w-4 shrink-0 text-[var(--color-muted-foreground)]",
              "transition-transform duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "group-data-[state=open]:rotate-180",
            )}
          />
        </DropdownMenuTrigger>

        <DropdownMenuContent
          align="start"
          style={{ minWidth }}
          className="py-1"
        >
          {allOptions.map((opt) => {
            const selected = opt.value === value;
            return (
              <DropdownMenuItem
                key={opt.value}
                onSelect={() => onChange(opt.value as T | "")}
                className={cn(
                  selected && [
                    // Rose-primary active state — tinted background + primary text
                    "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.08)]",
                    "text-[var(--color-primary)]",
                    "data-[highlighted]:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.12)]",
                    "data-[highlighted]:text-[var(--color-primary)]",
                  ],
                )}
              >
                <span className="flex-1 truncate">
                  {opt.label}
                  {opt.hint && (
                    <span className="ml-1.5 text-[var(--color-muted-foreground)] text-xs">
                      — {opt.hint}
                    </span>
                  )}
                </span>
                {selected && (
                  <Check
                    aria-hidden
                    className="ml-auto h-3.5 w-3.5 shrink-0 text-[var(--color-primary)]"
                  />
                )}
              </DropdownMenuItem>
            );
          })}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}

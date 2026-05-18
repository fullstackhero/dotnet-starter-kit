import * as React from "react";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/cn";

export type SelectOption<T extends string = string> = {
  value: T;
  label: string;
  hint?: string;
};

type SelectProps<T extends string = string> = Omit<
  React.SelectHTMLAttributes<HTMLSelectElement>,
  "onChange" | "value" | "children"
> & {
  value: T | "";
  onValueChange: (value: T | "") => void;
  options: SelectOption<T>[];
  /** Adds a leading "All" / "Any" / etc. option that maps to "". */
  emptyLabel?: string;
};

/**
 * Select — native <select> dressed in Console clothes. Picked over a
 * Radix popover combobox for v1: zero extra deps, keyboard-perfect
 * out of the box, scales to mobile. Wraps in a relative div so the
 * chevron sits as an absolute glyph (native <select> ignores ::after).
 */
export function Select<T extends string = string>({
  value,
  onValueChange,
  options,
  emptyLabel,
  className,
  id,
  disabled,
  ...rest
}: SelectProps<T>) {
  return (
    <div className={cn("relative", className)}>
      <select
        id={id}
        value={value}
        onChange={(e) => onValueChange(e.target.value as T | "")}
        disabled={disabled}
        className={cn(
          "h-9 w-full appearance-none rounded-md border border-[var(--color-input)] bg-transparent",
          "pl-3 pr-8 text-sm font-sans",
          "transition-[border-color,background-color,box-shadow] duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "hover:border-[var(--color-border-strong)]",
          "focus-visible:outline-none focus-visible:border-[var(--color-accent-signal)] focus-visible:ring-2 focus-visible:ring-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.25)] focus-visible:bg-[var(--color-surface-2)]",
          "disabled:cursor-not-allowed disabled:opacity-50",
          "aria-[invalid=true]:border-[var(--color-destructive)]",
        )}
        {...rest}
      >
        {emptyLabel !== undefined && <option value="">{emptyLabel}</option>}
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.hint ? `${opt.label} — ${opt.hint}` : opt.label}
          </option>
        ))}
      </select>
      <ChevronDown
        className="pointer-events-none absolute right-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--color-muted-foreground)]"
        aria-hidden
      />
    </div>
  );
}

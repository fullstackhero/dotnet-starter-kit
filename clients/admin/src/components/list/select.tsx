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

type SelectProps<T extends string = string> = {
  value: T | "";
  onValueChange: (value: T | "") => void;
  options: SelectOption<T>[];
  /** Adds a leading "All" / "Any" / etc. option that maps to "". */
  emptyLabel?: string;
  id?: string;
  disabled?: boolean;
  className?: string;
  "aria-invalid"?: boolean | "true" | "false";
  "aria-describedby"?: string;
};

/**
 * Select — form-field single-select built on the Radix DropdownMenu
 * primitive (shares the filter Select's vocabulary in components/ui).
 * Full-width trigger sized to its container with a brand-tinted focus
 * ring matching <Input>; the panel matches the trigger width and
 * scrolls for long lists. Keeps the former native-<select> props so
 * every call site is untouched, and forwards id + aria-* injected by
 * <Field>.
 */
export function Select<T extends string = string>({
  value,
  onValueChange,
  options,
  emptyLabel,
  id,
  disabled,
  className,
  "aria-invalid": ariaInvalid,
  "aria-describedby": ariaDescribedBy,
}: SelectProps<T>) {
  const allOptions: SelectOption<string>[] =
    emptyLabel !== undefined ? [{ value: "", label: emptyLabel }, ...options] : [...options];

  const current = allOptions.find((o) => o.value === value);
  const displayLabel = current
    ? current.hint
      ? `${current.label} — ${current.hint}`
      : current.label
    : emptyLabel ?? allOptions[0]?.label ?? "Select…";
  const hasSelection = value !== "";

  return (
    <div className={cn("relative", className)}>
      <DropdownMenu>
        <DropdownMenuTrigger
          id={id}
          disabled={disabled}
          aria-invalid={ariaInvalid}
          aria-describedby={ariaDescribedBy}
          className={cn(
            "group inline-flex h-9 w-full min-w-0 items-center justify-between gap-2 rounded-lg",
            "border border-[var(--color-input)] bg-transparent px-3 py-1 text-sm font-sans shadow-xs outline-none",
            hasSelection
              ? "text-[var(--color-foreground)]"
              : "text-[var(--color-muted-foreground)]",
            "transition-[color,box-shadow,border-color,background-color] duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
            "dark:bg-[oklch(from_var(--color-input)_l_c_h_/_0.3)]",
            "hover:border-[var(--color-border-strong)]",
            "focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
            "data-[state=open]:border-[var(--color-ring)] data-[state=open]:ring-[3px] data-[state=open]:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
            "aria-invalid:border-[var(--color-destructive)] aria-invalid:ring-[oklch(from_var(--color-destructive)_l_c_h_/_0.2)]",
            "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
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
          style={{
            width: "var(--radix-dropdown-menu-trigger-width)",
            minWidth: "var(--radix-dropdown-menu-trigger-width)",
          }}
        >
          {allOptions.map((opt) => {
            const selected = opt.value === value;
            return (
              <DropdownMenuItem
                key={opt.value || "__empty__"}
                onSelect={() => onValueChange(opt.value as T | "")}
                className={cn(
                  selected && [
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
                    <span className="ml-1.5 text-xs text-[var(--color-muted-foreground)]">
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

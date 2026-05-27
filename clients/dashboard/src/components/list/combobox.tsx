import { useEffect, useMemo, useRef, useState } from "react";
import { Check, ChevronDown, Search, X } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRow,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

export type ComboboxOption = {
  value: string;
  label: string;
  /** Optional left-of-label glyph (depth indent for trees, brand swatch, etc). */
  prefix?: React.ReactNode;
  /** Optional right-of-label muted hint (counts, slug, etc). */
  hint?: string;
};

type Variant = "filter" | "field";

/**
 * Combobox — popover-driven select that matches the dashboard's polished
 * design vocabulary. Replaces native <select> everywhere a list-page
 * filter or form field needs a chooser.
 *
 * Two visual variants:
 *   "filter" — small pill, used in toolbars. Active state tints primary.
 *   "field"  — full-width form input, used inside dialogs.
 *
 * Long lists become searchable with `searchable`. The search input
 * filters client-side and stops Radix from intercepting keystrokes.
 */
export function Combobox({
  label,
  value,
  onChange,
  options,
  placeholder,
  searchable = false,
  clearable = false,
  emptyOptionLabel,
  variant = "field",
  align = "start",
  disabled,
  id,
  className,
}: {
  label: string;
  value: string | null;
  onChange: (value: string | null) => void;
  options: ComboboxOption[];
  placeholder?: string;
  searchable?: boolean;
  clearable?: boolean;
  /** When provided, shows a "no value" option at the top of the list. */
  emptyOptionLabel?: string;
  variant?: Variant;
  align?: "start" | "end" | "center";
  disabled?: boolean;
  /** Accepted for API symmetry; the required indicator is rendered by the wrapping <Field>. */
  required?: boolean;
  id?: string;
  className?: string;
}) {
  const [open, setOpen] = useState(false);
  const [filter, setFilter] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);

  // Reset filter and focus search whenever the popover opens.
  useEffect(() => {
    if (open) {
      setFilter("");
      const t = setTimeout(() => inputRef.current?.focus(), 30);
      return () => clearTimeout(t);
    }
  }, [open]);

  const filtered = useMemo(() => {
    const q = filter.trim().toLowerCase();
    if (!q) return options;
    return options.filter((o) => o.label.toLowerCase().includes(q));
  }, [options, filter]);

  const selected = options.find((o) => o.value === value) ?? null;
  const hasValue = value !== null && value !== "";

  const showFieldClear = clearable && hasValue && !disabled;

  return (
    <DropdownMenu open={open} onOpenChange={(o) => !disabled && setOpen(o)}>
      {variant === "filter" ? (
        <DropdownMenuTrigger asChild disabled={disabled}>
          <FilterTrigger
            label={label}
            selected={selected}
            hasValue={hasValue}
            clearable={clearable}
            onClear={() => onChange(null)}
            disabled={disabled}
            className={className}
          />
        </DropdownMenuTrigger>
      ) : (
        // The clear control is a sibling of the trigger (not nested inside it):
        // an interactive element inside another interactive element is invalid
        // and unreliable for keyboard/AT.
        <div className="relative">
          <DropdownMenuTrigger asChild disabled={disabled}>
            <FieldTrigger
              id={id}
              placeholder={placeholder ?? `Select ${label.toLowerCase()}…`}
              selected={selected}
              hasValue={hasValue}
              hasClear={showFieldClear}
              disabled={disabled}
              className={className}
            />
          </DropdownMenuTrigger>
          {showFieldClear && (
            <button
              type="button"
              aria-label={`Clear ${label.toLowerCase()}`}
              onClick={() => onChange(null)}
              className="absolute right-8 top-1/2 grid h-5 w-5 -translate-y-1/2 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
            >
              <X className="h-3 w-3" />
            </button>
          )}
        </div>
      )}

      <DropdownMenuContent
        align={align}
        sideOffset={6}
        className={cn(
          "min-w-[var(--radix-dropdown-menu-trigger-width)]",
          "max-h-[min(360px,60vh)] overflow-hidden",
          "p-0",
        )}
      >
        {searchable && (
          <DropdownMenuRow className="border-b border-[var(--color-border)] gap-2 px-3 py-2.5">
            <Search className="h-3.5 w-3.5 text-[var(--color-muted-foreground)]" />
            <input
              ref={inputRef}
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              placeholder={`Filter ${label.toLowerCase()}…`}
              // Stop Radix's typeahead from swallowing the user's input.
              onKeyDown={(e) => {
                if (e.key !== "Escape") e.stopPropagation();
              }}
              className={cn(
                "h-6 w-full bg-transparent text-sm",
                "placeholder:text-[var(--color-muted-foreground)]",
                // The popover row is already a contained visual context — skip
                // the global :focus-visible halo so it doesn't draw a hard
                // rectangle + 6px outer bloom around the search input.
                "outline-none focus:outline-none focus-visible:outline-none focus-visible:shadow-none",
              )}
            />
            {filter && (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  setFilter("");
                  inputRef.current?.focus();
                }}
                className="grid h-5 w-5 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
                aria-label="Clear filter"
              >
                <X className="h-3 w-3" />
              </button>
            )}
          </DropdownMenuRow>
        )}

        <ul role="none" className="max-h-[300px] overflow-y-auto py-1">
          {emptyOptionLabel && (!filter || emptyOptionLabel.toLowerCase().includes(filter.toLowerCase())) && (
            <Option
              selected={value === null || value === ""}
              onPick={() => {
                onChange(null);
                setOpen(false);
              }}
            >
              <span className="text-[var(--color-muted-foreground)] italic">
                {emptyOptionLabel}
              </span>
            </Option>
          )}

          {filtered.length === 0 ? (
            <li className="px-3 py-3 text-center text-[12px] text-[var(--color-muted-foreground)]">
              No matches.
            </li>
          ) : (
            filtered.map((opt) => (
              <Option
                key={opt.value}
                selected={value === opt.value}
                onPick={() => {
                  onChange(opt.value);
                  setOpen(false);
                }}
              >
                {opt.prefix}
                <span className="flex-1 truncate">{opt.label}</span>
                {opt.hint && (
                  <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]/70">
                    {opt.hint}
                  </span>
                )}
              </Option>
            ))
          )}
        </ul>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function Option({
  selected,
  onPick,
  children,
}: {
  selected: boolean;
  onPick: () => void;
  children: React.ReactNode;
}) {
  return (
    <li role="none">
      <button
        type="button"
        onClick={onPick}
        role="menuitemradio"
        aria-checked={selected}
        className={cn(
          "group/opt flex w-full cursor-pointer items-center gap-2.5 px-3 py-1.5 text-left text-sm",
          "transition-colors duration-[var(--duration-fast)]",
          selected
            ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            : "text-[var(--color-foreground)] hover:bg-[var(--color-accent)]",
        )}
      >
        <Check
          className={cn(
            "h-3.5 w-3.5 shrink-0 transition-opacity",
            selected
              ? "text-[var(--color-primary)] opacity-100"
              : "opacity-0",
          )}
        />
        {children}
      </button>
    </li>
  );
}

// ─── Triggers ─────────────────────────────────────────────────────────

const FilterTrigger = ({
  label,
  selected,
  hasValue,
  clearable,
  onClear,
  disabled,
  className,
  ...props
}: {
  label: string;
  selected: ComboboxOption | null;
  hasValue: boolean;
  clearable: boolean;
  onClear: () => void;
  disabled?: boolean;
  className?: string;
} & React.ButtonHTMLAttributes<HTMLButtonElement>) => {
  return (
    <span className={cn("relative inline-flex items-center", className)}>
      <button
        type="button"
        disabled={disabled}
        aria-label={label}
        className={cn(
          "inline-flex h-7 cursor-pointer items-center gap-1.5 rounded-full pl-3 pr-2.5",
          "text-[11px] font-semibold uppercase tracking-wider",
          "border transition-colors duration-[var(--duration-fast)]",
          "focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.18)]",
          "disabled:cursor-not-allowed disabled:opacity-50",
          hasValue
            ? "border-[oklch(from_var(--color-primary)_l_c_h_/_0.25)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-primary)]"
            : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]",
          "data-[state=open]:bg-[var(--color-muted)] data-[state=open]:text-[var(--color-foreground)]",
        )}
        {...props}
      >
        <span className="opacity-70">{label.toUpperCase()}:</span>
        <span className="truncate">{selected?.label.toUpperCase() ?? "ALL"}</span>
        <ChevronDown
          aria-hidden
          className="h-3 w-3 transition-transform duration-[var(--duration-fast)] data-[state=open]:rotate-180"
        />
      </button>
      {clearable && hasValue && (
        <button
          type="button"
          aria-label={`Clear ${label} filter`}
          onClick={onClear}
          disabled={disabled}
          className="ml-1 grid h-5 w-5 cursor-pointer place-items-center rounded-full text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
        >
          <X className="h-3 w-3" />
        </button>
      )}
    </span>
  );
};

const FieldTrigger = ({
  id,
  placeholder,
  selected,
  hasValue,
  hasClear,
  disabled,
  className,
  ...props
}: {
  id?: string;
  placeholder: string;
  selected: ComboboxOption | null;
  hasValue: boolean;
  /** Reserve a slot for the sibling clear button so the label doesn't underlap it. */
  hasClear?: boolean;
  disabled?: boolean;
  className?: string;
} & React.ButtonHTMLAttributes<HTMLButtonElement>) => {
  return (
    // A plain menu-button: Radix's DropdownMenuTrigger forwards
    // aria-haspopup="menu" + aria-expanded via asChild, so no explicit
    // role is needed (and role="combobox" without aria-controls/-expanded
    // would be an invalid name/role/value pairing). `required` is conveyed
    // by the wrapping <Field> label, not aria-required (unsupported on a button).
    <button
      id={id}
      type="button"
      disabled={disabled}
      className={cn(
        "group/field relative flex h-9 w-full cursor-pointer items-center justify-between gap-2",
        "rounded-md border border-[var(--color-input)] bg-transparent px-3 text-left text-sm shadow-sm",
        "transition-colors duration-[var(--duration-fast)]",
        "hover:border-[var(--color-border-strong)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
        "data-[state=open]:border-[oklch(from_var(--color-primary)_l_c_h_/_0.4)]",
        "data-[state=open]:ring-2 data-[state=open]:ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)] data-[state=open]:ring-offset-0",
        "disabled:cursor-not-allowed disabled:opacity-50",
        className,
      )}
      {...props}
    >
      <span className="flex min-w-0 flex-1 items-center gap-2">
        {selected?.prefix}
        <span
          className={cn(
            "truncate",
            hasValue
              ? "text-[var(--color-foreground)]"
              : "text-[var(--color-muted-foreground)]",
          )}
        >
          {selected?.label ?? placeholder}
        </span>
      </span>
      <span className="flex shrink-0 items-center gap-1">
        {/* Reserve room for the sibling clear button so the label can't run under it. */}
        {hasClear && <span aria-hidden className="h-5 w-5" />}
        <ChevronDown
          aria-hidden
          className={cn(
            "h-4 w-4 text-[var(--color-muted-foreground)]",
            "transition-transform duration-[var(--duration-fast)]",
            "group-data-[state=open]/field:rotate-180",
          )}
        />
      </span>
    </button>
  );
};

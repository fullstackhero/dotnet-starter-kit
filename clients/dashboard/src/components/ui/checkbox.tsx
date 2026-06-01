import * as React from "react";
import { Check } from "lucide-react";
import { cn } from "@/lib/cn";

export type CheckboxProps = Omit<
  React.InputHTMLAttributes<HTMLInputElement>,
  "type" | "size"
>;

/**
 * Checkbox — styled native <input type="checkbox"> (no Radix dependency).
 * Mirrors the hand-rolled custom checkbox used across role-detail /
 * group-detail: a 16px rounded box that fills with the primary colour and
 * shows a Check glyph when checked, with the same 3px brand focus ring as
 * Input. The real input stays in the DOM (sr-only, peer) so the control is
 * fully keyboard- and form-accessible; the visible box is a sibling that
 * reacts to the input's :checked / :focus-visible / :disabled states.
 *
 * Usage: pair with a <label> (or wrap in one) for a clickable hit area —
 *   <label className="flex items-center gap-2">
 *     <Checkbox checked={on} onChange={…} /> <span>Label</span>
 *   </label>
 */
export const Checkbox = React.forwardRef<HTMLInputElement, CheckboxProps>(
  ({ className, ...props }, ref) => {
    return (
      <span className="relative inline-grid size-4 shrink-0 place-items-center">
        <input
          ref={ref}
          type="checkbox"
          data-slot="checkbox"
          className={cn(
            "peer absolute inset-0 size-full cursor-pointer appearance-none rounded",
            "disabled:cursor-not-allowed",
            className,
          )}
          {...props}
        />
        <span
          aria-hidden
          className={cn(
            "pointer-events-none grid size-4 place-items-center rounded border transition-colors",
            "border-[var(--color-input)] bg-transparent text-transparent",
            "peer-checked:border-[var(--color-primary)] peer-checked:bg-[var(--color-primary)] peer-checked:text-[var(--color-primary-foreground)]",
            "peer-focus-visible:border-[var(--color-ring)] peer-focus-visible:ring-[3px] peer-focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
            "peer-disabled:opacity-50",
          )}
        >
          <Check className="size-3" />
        </span>
      </span>
    );
  },
);
Checkbox.displayName = "Checkbox";

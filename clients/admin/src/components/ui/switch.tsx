import * as React from "react";
import { cn } from "@/lib/cn";

type SwitchProps = {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled?: boolean;
  id?: string;
  "aria-label"?: string;
  "aria-labelledby"?: string;
};

/**
 * Switch — accessible toggle. Tracks a controlled boolean and slides a
 * thumb between off and on. Keyboard: space / enter activates (button
 * default). The thumb position is tweened with motion tokens.
 */
export const Switch = React.forwardRef<HTMLButtonElement, SwitchProps>(
  ({ checked, onCheckedChange, disabled, ...props }, ref) => {
    return (
      <button
        ref={ref}
        type="button"
        role="switch"
        aria-checked={checked}
        disabled={disabled}
        onClick={() => onCheckedChange(!checked)}
        className={cn(
          "relative inline-flex h-5 w-9 shrink-0 cursor-pointer items-center rounded-full",
          "transition-colors duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--color-background)]",
          "disabled:cursor-not-allowed disabled:opacity-50",
          checked
            ? "bg-[var(--color-primary)]"
            : "bg-[var(--color-input)]",
        )}
        {...props}
      >
        <span
          aria-hidden
          className={cn(
            "pointer-events-none inline-block h-4 w-4 rounded-full bg-[var(--color-overlay-foreground)] shadow-[0_1px_2px_oklch(0_0_0_/_0.20)]",
            "transition-transform duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
            checked ? "translate-x-[18px]" : "translate-x-0.5",
          )}
        />
      </button>
    );
  },
);
Switch.displayName = "Switch";

import * as React from "react";
import { cn } from "@/lib/cn";

export type InputProps = React.InputHTMLAttributes<HTMLInputElement>;

/**
 * Input — flat bottom-border-emphasized field in the Console language.
 * Resting state: hairline border, transparent fill. Hover: stronger border.
 * Focus: signal-colored ring + subtle background lift. No floating labels;
 * label sits above via the Label primitive.
 */
export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, ...props }, ref) => {
    return (
      <input
        type={type}
        ref={ref}
        className={cn(
          "flex h-9 w-full rounded-md border border-[var(--color-input)] bg-transparent",
          "px-3 py-1 text-sm font-sans",
          "placeholder:text-[var(--color-muted-foreground)] placeholder:font-mono placeholder:text-xs placeholder:tracking-tight",
          "transition-[border-color,background-color,box-shadow] duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "hover:border-[var(--color-border-strong)]",
          "focus-visible:outline-none focus-visible:border-[var(--color-accent-signal)] focus-visible:ring-2 focus-visible:ring-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.25)] focus-visible:bg-[var(--color-surface-2)]",
          "disabled:cursor-not-allowed disabled:opacity-50",
          "aria-[invalid=true]:border-[var(--color-destructive)] aria-[invalid=true]:focus-visible:ring-[oklch(from_var(--color-destructive)_l_c_h_/_0.25)]",
          className,
        )}
        {...props}
      />
    );
  },
);
Input.displayName = "Input";

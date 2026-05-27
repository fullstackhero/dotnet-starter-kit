import * as React from "react";
import { cn } from "@/lib/cn";

export type InputProps = React.InputHTMLAttributes<HTMLInputElement>;

/**
 * Input — h-9 hairline-bordered field with a 3px brand-tinted focus
 * ring. Sits on transparent so it inherits the surface it's placed on
 * (card vs. canvas); in dark mode picks up a faint input tint so
 * inputs read as recessed wells on graphite. No floating-label
 * monster — labels live above the field as plain <Label>s.
 */
export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, ...props }, ref) => {
    return (
      <input
        type={type}
        data-slot="input"
        className={cn(
          "h-9 w-full min-w-0 rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-1",
          "text-sm shadow-xs outline-none",
          "transition-[color,box-shadow,border-color,background-color] duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "selection:bg-[var(--color-primary)] selection:text-[var(--color-primary-foreground)]",
          "file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-[var(--color-foreground)]",
          "placeholder:text-[var(--color-muted-foreground)]",
          "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
          "md:text-sm",
          "dark:bg-[oklch(from_var(--color-input)_l_c_h_/_0.3)]",
          "focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
          "aria-invalid:border-[var(--color-destructive)] aria-invalid:ring-[oklch(from_var(--color-destructive)_l_c_h_/_0.2)]",
          className,
        )}
        ref={ref}
        {...props}
      />
    );
  },
);
Input.displayName = "Input";

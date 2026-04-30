import * as React from "react";
import { cn } from "@/lib/cn";

export type InputProps = React.InputHTMLAttributes<HTMLInputElement>;

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, ...props }, ref) => {
    return (
      <input
        type={type}
        className={cn(
          "flex h-9 w-full rounded-md border border-[var(--color-input)] bg-[var(--color-surface-2)] px-3 py-1 text-sm shadow-[var(--shadow-xs)]",
          "transition-[border-color,box-shadow,background-color] duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "placeholder:text-[var(--color-muted-foreground)]",
          "hover:border-[var(--color-border-strong)]",
          // Defer focus styling to the global :focus-visible halo
          // (outline + outer bloom) so we don't paint a second border.
          "focus-visible:border-[var(--color-input)]",
          "disabled:cursor-not-allowed disabled:opacity-50",
          className,
        )}
        ref={ref}
        {...props}
      />
    );
  },
);
Input.displayName = "Input";

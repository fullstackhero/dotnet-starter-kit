import * as React from "react";
import { cn } from "@/lib/cn";

export type TextareaProps = React.TextareaHTMLAttributes<HTMLTextAreaElement>;

/**
 * Textarea — multi-line sibling of <Input>. Same hairline border, 3px
 * brand-tinted focus ring, transparent surface (so it inherits card vs.
 * canvas), and faint dark-mode well. Defaults to a 3-row min height with
 * vertical-only resize so it never breaks a dialog's horizontal rhythm.
 * Labels live above via <Label>/<Field>, exactly like Input.
 */
export const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, rows = 3, ...props }, ref) => {
    return (
      <textarea
        ref={ref}
        rows={rows}
        data-slot="textarea"
        className={cn(
          "w-full min-w-0 rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2",
          "text-sm shadow-xs outline-none",
          "field-sizing-content min-h-16 resize-y",
          "transition-[color,box-shadow,border-color,background-color] duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "selection:bg-[var(--color-primary)] selection:text-[var(--color-primary-foreground)]",
          "placeholder:text-[var(--color-muted-foreground)]",
          "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
          "md:text-sm",
          "dark:bg-[oklch(from_var(--color-input)_l_c_h_/_0.3)]",
          "focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
          "aria-invalid:border-[var(--color-destructive)] aria-invalid:ring-[oklch(from_var(--color-destructive)_l_c_h_/_0.2)]",
          className,
        )}
        {...props}
      />
    );
  },
);
Textarea.displayName = "Textarea";

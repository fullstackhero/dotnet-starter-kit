import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/cn";

/**
 * Badge — compact status pill. Variants map to semantic tokens so a
 * brand re-tone propagates without touching call sites. The `soft`
 * style uses the matching `*-soft` background where defined and falls
 * back to a tinted layer otherwise.
 *
 * Admin-specific variant `muted` is preserved for call-site compat.
 */
const badgeVariants = cva(
  "inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-[11px] font-medium tracking-tight whitespace-nowrap",
  {
    variants: {
      variant: {
        default:
          "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-foreground)]",
        brand:
          "border-transparent bg-[var(--color-primary-soft)] text-[var(--color-primary)]",
        // `muted` kept for admin call-site compat.
        muted:
          "border-transparent bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
        success:
          "border-transparent bg-[oklch(from_var(--color-success)_l_c_h_/_0.14)] text-[var(--color-success)]",
        warning:
          "border-transparent bg-[oklch(from_var(--color-warning)_l_c_h_/_0.16)] text-[var(--color-warning)]",
        info:
          "border-transparent bg-[oklch(from_var(--color-info)_l_c_h_/_0.14)] text-[var(--color-info)]",
        danger:
          "border-transparent bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.14)] text-[var(--color-destructive)]",
        outline:
          "border-[var(--color-border-strong)] bg-transparent text-[var(--color-muted-foreground)]",
      },
    },
    defaultVariants: { variant: "default" },
  },
);

export interface BadgeProps
  extends React.HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {}

export function Badge({ className, variant, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ variant }), className)} {...props} />;
}

export { badgeVariants };

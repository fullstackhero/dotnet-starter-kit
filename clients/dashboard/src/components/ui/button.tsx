import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/cn";

/**
 * Button — single source of truth for clickable actions. Variants are
 * tuned to the dashboard's premium-SaaS aesthetic:
 *   - default: solid brand fill with a faint top-edge sheen and a brand
 *     halo on hover. Reads as "the one thing on this surface."
 *   - outline: 1px hairline that swaps to a soft brand tint on hover.
 *   - ghost / link: chrome-free for in-flow actions.
 *
 * Radius and motion timing come from the design tokens so any global
 * tuning propagates without touching this file.
 */
const buttonVariants = cva(
  [
    "relative inline-flex cursor-pointer select-none items-center justify-center gap-2 whitespace-nowrap",
    "rounded-md text-sm font-medium tracking-tight",
    "transition-[background-color,box-shadow,transform,color] duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--color-background)]",
    "disabled:cursor-not-allowed disabled:pointer-events-none disabled:opacity-50",
    "active:translate-y-px",
  ].join(" "),
  {
    variants: {
      variant: {
        default: [
          "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]",
          "shadow-[0_1px_0_oklch(1_0_0_/_0.12)_inset,0_1px_2px_oklch(0_0_0_/_0.12)]",
          "hover:bg-[var(--color-primary-hover)]",
          "hover:shadow-[0_1px_0_oklch(1_0_0_/_0.18)_inset,0_4px_18px_-6px_oklch(from_var(--color-primary)_l_c_h_/_0.55)]",
        ].join(" "),
        destructive: [
          "bg-[var(--color-destructive)] text-[var(--color-destructive-foreground)]",
          "shadow-[0_1px_0_oklch(1_0_0_/_0.12)_inset,0_1px_2px_oklch(0_0_0_/_0.12)]",
          "hover:brightness-[1.05]",
        ].join(" "),
        outline: [
          "border border-[var(--color-border-strong)] bg-[var(--color-surface-2)] text-[var(--color-foreground)]",
          "hover:bg-[var(--color-surface-4)] hover:border-[var(--color-border-strong)]",
        ].join(" "),
        secondary:
          "bg-[var(--color-secondary)] text-[var(--color-secondary-foreground)] hover:bg-[var(--color-surface-4)]",
        ghost:
          "text-[var(--color-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-accent-foreground)]",
        link: "text-[var(--color-primary)] underline-offset-4 hover:underline",
        soft: "bg-[var(--color-primary-soft)] text-[var(--color-primary)] hover:brightness-[1.08]",
      },
      size: {
        default: "h-9 px-4 py-2",
        sm: "h-8 rounded-md px-3 text-xs",
        lg: "h-10 rounded-md px-6",
        icon: "h-9 w-9",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  },
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button";
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    );
  },
);
Button.displayName = "Button";

export { buttonVariants };

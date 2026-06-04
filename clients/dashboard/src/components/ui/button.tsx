import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/cn";

/**
 * Button — refined warm-paper variants. The default solid button reads
 * as a stamp pressed into paper: solid brand fill, subtle top-edge
 * highlight, hover dims to 90%. Outline is a hairline that warms on
 * hover. No shimmer, no halo — the discipline IS the design.
 */
const buttonVariants = cva(
  [
    "inline-flex shrink-0 cursor-pointer select-none items-center justify-center gap-2 whitespace-nowrap",
    "rounded-md text-sm font-medium font-sans tracking-tight",
    "transition-all duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
    "outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)] focus-visible:border-[var(--color-ring)]",
    "disabled:pointer-events-none disabled:opacity-50",
    "[&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
    "active:scale-[0.98]",
  ].join(" "),
  {
    variants: {
      variant: {
        default:
          "bg-[var(--color-primary)] text-[var(--color-primary-foreground)] hover:brightness-[1.05] hover:shadow-[0_4px_18px_-6px_oklch(from_var(--color-primary)_l_c_h_/_0.45)]",
        destructive:
          "bg-[var(--color-destructive)] text-[var(--color-destructive-foreground)] hover:brightness-[1.05]",
        outline: [
          "border border-[var(--color-input)] bg-[var(--color-card)] text-[var(--color-foreground)] shadow-xs",
          "hover:bg-[var(--color-accent)] hover:text-[var(--color-accent-foreground)]",
        ].join(" "),
        secondary:
          "bg-[var(--color-secondary)] text-[var(--color-secondary-foreground)] hover:opacity-90",
        ghost:
          "text-[var(--color-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-accent-foreground)]",
        link: "text-[var(--color-primary)] underline-offset-4 hover:underline",
        soft: "bg-[var(--color-primary-soft)] text-[var(--color-primary)] hover:brightness-[1.06]",
        saffron:
          "bg-[var(--color-saffron)] text-[var(--color-saffron-foreground)] hover:brightness-[1.05] hover:shadow-[0_4px_18px_-6px_oklch(from_var(--color-saffron)_l_c_h_/_0.45)]",
      },
      size: {
        default: "h-9 px-4 py-2 has-[>svg]:px-3",
        xs: "h-7 gap-1 rounded-md px-2 text-xs has-[>svg]:px-1.5 [&_svg:not([class*='size-'])]:size-3",
        sm: "h-8 gap-1.5 rounded-md px-3 has-[>svg]:px-2.5",
        lg: "h-10 rounded-md px-6 has-[>svg]:px-4",
        icon: "size-9",
        "icon-xs": "size-7 rounded-md [&_svg:not([class*='size-'])]:size-3.5",
        "icon-sm": "size-9",
        "icon-lg": "size-10",
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
        data-slot="button"
        data-variant={variant ?? "default"}
        data-size={size ?? "default"}
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    );
  },
);
Button.displayName = "Button";

export { buttonVariants };

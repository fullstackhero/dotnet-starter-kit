import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/cn";

/**
 * Button — Console primitives.
 *
 * Variants:
 *   • default — solid primary (near-black/near-white based on theme).
 *   • signal  — chartreuse accent. The hero CTA, used SPARINGLY.
 *               Reserved for single primary actions per surface.
 *   • outline — hairline border, transparent fill. Default neutral action.
 *   • secondary — filled secondary surface.
 *   • ghost   — text-only, hover surfaces the accent background.
 *   • destructive — danger tone.
 *   • link    — inline text link.
 */
const buttonVariants = cva(
  [
    "inline-flex items-center justify-center gap-2 whitespace-nowrap",
    "rounded-md text-sm font-medium",
    "transition-[background-color,color,border-color,box-shadow,transform]",
    "duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--color-background)]",
    "disabled:pointer-events-none disabled:opacity-50",
    "active:scale-[0.985]",
  ].join(" "),
  {
    variants: {
      variant: {
        default:
          "bg-[var(--color-primary)] text-[var(--color-primary-foreground)] hover:bg-[oklch(from_var(--color-primary)_calc(l_-_0.04)_c_h)]",
        signal:
          "bg-[var(--color-accent-signal)] text-[var(--color-accent-signal-foreground)] shadow-[0_0_0_1px_oklch(from_var(--color-accent-signal)_calc(l_-_0.10)_c_h)] hover:bg-[oklch(from_var(--color-accent-signal)_calc(l_-_0.04)_c_h)]",
        destructive:
          "bg-[var(--color-destructive)] text-[var(--color-destructive-foreground)] hover:opacity-90",
        outline:
          "border border-[var(--color-border-strong)] bg-transparent text-[var(--color-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-accent-foreground)]",
        secondary:
          "bg-[var(--color-secondary)] text-[var(--color-secondary-foreground)] hover:bg-[var(--color-accent)]",
        ghost:
          "text-[var(--color-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-accent-foreground)]",
        link:
          "text-[var(--color-foreground)] underline-offset-4 hover:underline decoration-[var(--color-accent-signal)] decoration-2",
      },
      size: {
        default: "h-9 px-4 py-2",
        sm: "h-8 rounded-md px-3 text-xs",
        lg: "h-10 rounded-md px-5",
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
      <Comp className={cn(buttonVariants({ variant, size, className }))} ref={ref} {...props} />
    );
  },
);
Button.displayName = "Button";

export { buttonVariants };

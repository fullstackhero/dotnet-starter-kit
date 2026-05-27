import * as React from "react";
import { cn } from "@/lib/cn";

type CardProps = React.HTMLAttributes<HTMLDivElement> & {
  /** When true, the card softens its border + shadow on hover. */
  interactive?: boolean;
};

/**
 * Card — calm neutral surface. A 1px hairline + a low resting
 * shadow that lifts the card off the canvas just enough to read as
 * "sheet on a desk." Interactive variant darkens the border and adds
 * a hint of lift on hover — no pillow shadows, no gradient borders.
 *
 * Note: `card-shell` and `card-shell-interactive` CSS utility classes
 * (defined in globals.css) remain available for direct className usage
 * in legacy admin consumers during phase migration.
 */
export const Card = React.forwardRef<HTMLDivElement, CardProps>(
  ({ className, interactive, ...props }, ref) => (
    <div
      ref={ref}
      data-slot="card"
      className={cn(
        "flex flex-col rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-card-foreground)] shadow-sm",
        interactive &&
          "transition-[border-color,box-shadow,transform] duration-[var(--duration-default)] ease-[var(--ease-out-cubic)] hover:border-[var(--color-border-strong)] hover:shadow-md hover:-translate-y-px",
        className,
      )}
      {...props}
    />
  ),
);
Card.displayName = "Card";

export const CardHeader = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      data-slot="card-header"
      className={cn("flex flex-col gap-1.5 px-6 pt-6 pb-3", className)}
      {...props}
    />
  ),
);
CardHeader.displayName = "CardHeader";

export const CardTitle = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      data-slot="card-title"
      className={cn("font-display text-base font-semibold leading-none tracking-tight", className)}
      {...props}
    />
  ),
);
CardTitle.displayName = "CardTitle";

export const CardDescription = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      data-slot="card-description"
      className={cn("text-sm leading-relaxed text-[var(--color-muted-foreground)]", className)}
      {...props}
    />
  ),
);
CardDescription.displayName = "CardDescription";

export const CardContent = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} data-slot="card-content" className={cn("px-6 pb-6", className)} {...props} />
  ),
);
CardContent.displayName = "CardContent";

export const CardFooter = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      data-slot="card-footer"
      className={cn("flex items-center gap-2 border-t border-[var(--color-border)] px-6 py-4", className)}
      {...props}
    />
  ),
);
CardFooter.displayName = "CardFooter";

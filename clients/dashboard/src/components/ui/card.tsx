import * as React from "react";
import { cn } from "@/lib/cn";

type CardProps = React.HTMLAttributes<HTMLDivElement> & {
  /** When true, the card softens its shadow in on hover. */
  interactive?: boolean;
};

/**
 * Card — primary content surface, modernized.
 *
 * Earlier revisions stacked a luminance-ramped gradient border, an inset
 * top-edge highlight ("glossy lip"), and a double drop-shadow on hover.
 * The combination read as 2014-era skeuomorphism on dense pages.
 *
 * Now: a single hairline border at low alpha, no resting shadow at all
 * (depth comes from surface-tier contrast — surface-1 on surface-2 on
 * surface-3 — not chrome), and on `interactive` a soft pillow-shadow
 * that fades in slowly with a quiet border-strong tint. Linear / Mercury
 * vocabulary. The `card-shell` utility owns the styling so we can tune
 * it in one place.
 */
export const Card = React.forwardRef<HTMLDivElement, CardProps>(
  ({ className, interactive, ...props }, ref) => (
    <div
      ref={ref}
      className={cn(
        "card-shell text-[var(--color-card-foreground)]",
        interactive && "card-shell-interactive cursor-default",
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
      className={cn("flex flex-col gap-1.5 px-6 pt-5 pb-3", className)}
      {...props}
    />
  ),
);
CardHeader.displayName = "CardHeader";

export const CardTitle = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      className={cn("text-base font-semibold leading-none tracking-tight", className)}
      {...props}
    />
  ),
);
CardTitle.displayName = "CardTitle";

export const CardDescription = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      className={cn("text-sm leading-relaxed text-[var(--color-muted-foreground)]", className)}
      {...props}
    />
  ),
);
CardDescription.displayName = "CardDescription";

export const CardContent = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn("px-6 pb-5 pt-1", className)} {...props} />
  ),
);
CardContent.displayName = "CardContent";

export const CardFooter = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      className={cn("flex items-center gap-2 border-t border-[var(--color-border)] px-6 py-3", className)}
      {...props}
    />
  ),
);
CardFooter.displayName = "CardFooter";

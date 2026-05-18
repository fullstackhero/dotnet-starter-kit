import * as React from "react";
import { cn } from "@/lib/cn";

/**
 * Skeleton — placeholder block with a shimmer sweep. Use instead of
 * "Loading…" copy for any content that has predictable dimensions
 * (rows, KPI tiles, charts). The shimmer keyframe lives in globals.css
 * and honours prefers-reduced-motion.
 */
export const Skeleton = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn("skeleton h-4 w-full", className)} {...props} />
  ),
);
Skeleton.displayName = "Skeleton";

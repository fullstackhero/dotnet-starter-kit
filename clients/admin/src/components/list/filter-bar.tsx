import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

type FilterBarProps = {
  children: ReactNode;
  trailing?: ReactNode;
  className?: string;
};

/**
 * FilterBar — horizontal rail of filters used above lists. Mono-caps
 * "FILTERS" eyebrow on the left, filters in the middle, optional
 * trailing slot (e.g. clear-all link, density toggle). Wraps on small
 * screens so it never overflows.
 */
export function FilterBar({ children, trailing, className }: FilterBarProps) {
  return (
    <div
      className={cn(
        "flex flex-wrap items-center gap-x-3 gap-y-2 border-y border-[var(--color-border)] px-1 py-3",
        className,
      )}
    >
      <span className="meta text-[var(--color-muted-foreground)]">// Filters</span>
      <div className="flex flex-1 flex-wrap items-center gap-2">{children}</div>
      {trailing && <div className="flex items-center gap-2">{trailing}</div>}
    </div>
  );
}

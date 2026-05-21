import { cn } from "@/lib/cn";

export type StatTone = "default" | "warning" | "danger";

/**
 * Atomic stat card used by every list-page stat strip. Calm warm-paper
 * card with a plain uppercase label, a tabular value, and a hint line.
 * `tone` shifts the value color.
 */
export function Stat({
  label,
  value,
  hint,
  accent,
  tone = "default",
}: {
  label: string;
  value: React.ReactNode;
  hint: string;
  accent?: boolean;
  tone?: StatTone;
}) {
  // Uniform height: every Stat in a strip lays out as
  // [label · 1 line] / [value · fixed leading] / [hint · 1 line truncated],
  // and the outer container fills any remaining grid-row height via
  // `h-full`. The result is a perfectly aligned strip even when the
  // parent grid stretches one tile taller than its content.
  return (
    <div
      className={cn(
        "flex h-full min-h-[92px] flex-col justify-between rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs",
        "px-5 py-4",
      )}
    >
      <div className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
        {label}
      </div>
      <div
        className={cn(
          "font-display mt-2 text-[22px] font-semibold leading-none tracking-tight tabular-nums text-[var(--color-foreground)]",
          accent && "text-[var(--color-primary)]",
          tone === "warning" && "text-[var(--color-warning)]",
          tone === "danger" && "text-[var(--color-destructive)]",
        )}
      >
        {value}
      </div>
      <div className="mt-1 truncate text-[12px] text-[var(--color-muted-foreground)]">
        {hint}
      </div>
    </div>
  );
}

/**
 * Layout shell — `fsh-enter fsh-enter-2` stagger + responsive grid.
 * Pages compose any number of <Stat /> children inside.
 */
export function StatStrip({
  cols = 3,
  children,
}: {
  cols?: 2 | 3 | 4;
  children: React.ReactNode;
}) {
  return (
    <div
      className={cn(
        "fsh-enter fsh-enter-2 grid grid-cols-1 gap-3",
        cols === 2 && "sm:grid-cols-2",
        cols === 3 && "sm:grid-cols-3",
        cols === 4 && "sm:grid-cols-2 xl:grid-cols-4",
      )}
    >
      {children}
    </div>
  );
}

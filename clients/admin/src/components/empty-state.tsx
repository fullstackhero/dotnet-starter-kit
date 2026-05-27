import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";

type EmptyStateProps = {
  icon?: LucideIcon;
  /** Mono crumb shown above the headline. */
  kicker?: string;
  title: string;
  description?: React.ReactNode;
  action?: React.ReactNode;
  className?: string;
};

/**
 * EmptyState — used wherever a list/query returns no results. Pulls the
 * Console language together in one place: hairline icon container, mono
 * kicker, display headline, single CTA. Pages should reach for this
 * instead of inline "No results" copy.
 */
export function EmptyState({
  icon: Icon,
  kicker,
  title,
  description,
  action,
  className,
}: EmptyStateProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-4 px-6 py-14 text-center",
        className,
      )}
    >
      {Icon && (
        <span
          aria-hidden
          className="relative grid h-14 w-14 place-items-center rounded-xl border border-[var(--color-border)] bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)] shadow-[inset_0_1px_0_oklch(1_0_0_/_0.55)]"
        >
          <Icon className="h-5 w-5" />
          <span
            aria-hidden
            className="pointer-events-none absolute -bottom-1.5 left-1/2 h-px w-8 -translate-x-1/2 bg-[var(--color-accent-signal)] opacity-40"
          />
        </span>
      )}
      {kicker && (
        <span className="meta text-[var(--color-muted-foreground)]">{kicker}</span>
      )}
      <h3 className="font-display text-2xl font-semibold tracking-tight">{title}</h3>
      {description && (
        <p className="max-w-md text-sm text-[var(--color-muted-foreground)] leading-relaxed">
          {description}
        </p>
      )}
      {action && <div className="pt-1">{action}</div>}
    </div>
  );
}

import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

/**
 * Calm page hero used across non-list surfaces (health, audit trail,
 * sessions, trash, settings). Shares the same calm warm-paper chrome as
 * `ListHero` but omits the built-in search input and CTA button.
 * Callers slot in their own actions on the right.
 *
 * Composition:
 *   eyebrow row  — section · area · tenant · sub-eyebrow
 *   title row    — display heading + optional badge + actions slot
 *   subtitle     — muted body line below
 */
export function PageHero({
  eyebrow,
  tenant,
  subEyebrow,
  title,
  badge,
  subtitle,
  actions,
  className,
}: {
  eyebrow: string;
  tenant?: string;
  subEyebrow?: string;
  title: string;
  /** Optional after-title element — pill, count, status indicator. */
  badge?: ReactNode;
  /** Muted body line under the title. Plain string or rich React. */
  subtitle?: ReactNode;
  /** Right-aligned actions (buttons, refresh, etc.). */
  actions?: ReactNode;
  className?: string;
}) {
  return (
    <section
      className={cn(
        "fsh-enter fsh-enter-1 overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs",
        className,
      )}
    >
      <div className="relative px-6 py-6 sm:px-8 sm:py-7 md:px-8">
        {/* Eyebrow row */}
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1.5">
          <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            {eyebrow}
          </span>
          {tenant && (
            <>
              <span aria-hidden className="h-px w-7 bg-[var(--color-border)]" />
              <code className="rounded bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] px-1.5 py-0.5 text-[11px] font-medium text-[var(--color-primary)]">
                {tenant}
              </code>
            </>
          )}
          {subEyebrow && (
            <>
              <span aria-hidden className="hidden h-px w-7 bg-[var(--color-border)] sm:inline-block" />
              <span className="hidden text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] sm:inline">
                {subEyebrow}
              </span>
            </>
          )}
        </div>

        {/* Title + actions */}
        <div className="mt-4 grid grid-cols-1 gap-6 md:grid-cols-[1fr_auto] md:items-end">
          <div className="min-w-0">
            <h1 className="font-display flex flex-wrap items-baseline gap-3 pb-1 text-display-page font-semibold leading-[1.15] tracking-tight text-[var(--color-foreground)]">
              {title}
              {badge}
            </h1>
            {subtitle && (
              <p className="mt-1.5 max-w-2xl text-[13.5px] leading-relaxed text-[var(--color-muted-foreground)]">
                {subtitle}
              </p>
            )}
          </div>

          {actions && (
            <div className="flex flex-wrap items-center gap-2 md:justify-end">
              {actions}
            </div>
          )}
        </div>
      </div>
    </section>
  );
}

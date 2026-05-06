import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

/**
 * Atmospheric page hero used across non-list surfaces (health, audit
 * trail, sessions, trash, settings). Shares the same chrome as
 * `ListHero` — radial brand wash + soft noise overlay — but omits the
 * built-in search input and CTA button. Callers slot in their own
 * actions on the right.
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
        "fsh-enter fsh-enter-1 card-shell relative overflow-hidden rounded-[20px]",
        "bg-[var(--color-surface-3)]",
        className,
      )}
    >
      {/* Brand wash — three soft radial gradients. Same recipe as
          ListHero so the two visually unify. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage: `
            radial-gradient(60% 70% at 0% 0%, oklch(from var(--color-primary) l c h / 0.18), transparent 60%),
            radial-gradient(50% 60% at 100% 0%, oklch(0.700 0.155 195 / 0.10), transparent 65%),
            radial-gradient(80% 80% at 100% 100%, oklch(from var(--color-primary) l c h / 0.05), transparent 70%)
          `,
        }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.06] mix-blend-overlay"
        style={{
          backgroundImage:
            "url(\"data:image/svg+xml;utf8,<svg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='2' stitchTiles='stitch'/></filter><rect width='100%' height='100%' filter='url(%23n)'/></svg>\")",
        }}
      />

      <div className="relative px-6 py-7 sm:px-8 sm:py-9 md:px-10">
        {/* Eyebrow row */}
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1.5">
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            {eyebrow}
          </span>
          {tenant && (
            <>
              <span aria-hidden className="h-px w-7 bg-[var(--color-border-strong)]" />
              <code className="rounded bg-[var(--color-primary-soft)] px-1.5 py-0.5 font-mono text-[11px] font-medium text-[var(--color-primary)]">
                {tenant}
              </code>
            </>
          )}
          {subEyebrow && (
            <>
              <span aria-hidden className="hidden h-px w-7 bg-[var(--color-border-strong)] sm:inline-block" />
              <span className="hidden font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]/80 sm:inline">
                {subEyebrow}
              </span>
            </>
          )}
        </div>

        {/* Title + actions */}
        <div className="mt-5 grid grid-cols-1 gap-6 md:grid-cols-[1fr_auto] md:items-end">
          <div className="min-w-0">
            <h1 className="text-display flex items-baseline gap-3 text-[34px] font-semibold leading-[1.04] tracking-[-0.022em] sm:text-[40px]">
              <span className="truncate">{title}</span>
              {badge}
            </h1>
            {subtitle && (
              <p className="mt-2 max-w-2xl text-[14px] leading-relaxed text-[var(--color-muted-foreground)]">
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

import type { ReactNode } from "react";
import { BrandMarkXL } from "@/components/brand-mark";
import { cn } from "@/lib/cn";

// ────────────────────────────────────────────────────────────────────────
// AuthShell — shared chrome for unauthenticated admin surfaces
// (login already inlines its own variant; this is for forgot-password,
// reset-password, confirm-email).
//
// Mirrors login.tsx's editorial split-screen aesthetic:
//   left pane (lg+):  brand stage — canvas-mesh, chartreuse vignette,
//                     corner ticks, BrandMarkXL hero monogram.
//   right pane:       focused form column with the // SECTION-RULE chip.
//
// The brand stage stays consistent across all auth pages so the operator
// always knows they're on the FSH Console surface. The right pane carries
// the page-specific content.
// ────────────────────────────────────────────────────────────────────────

function CornerTicks() {
  const TICK = "h-3 w-3 border-[var(--color-accent-signal)]";
  return (
    <>
      <span className={cn("pointer-events-none absolute left-6 top-6 border-l-2 border-t-2", TICK)} aria-hidden />
      <span className={cn("pointer-events-none absolute right-6 top-6 border-r-2 border-t-2", TICK)} aria-hidden />
      <span className={cn("pointer-events-none absolute left-6 bottom-6 border-l-2 border-b-2", TICK)} aria-hidden />
      <span className={cn("pointer-events-none absolute right-6 bottom-6 border-r-2 border-b-2", TICK)} aria-hidden />
    </>
  );
}

export function AuthShell({
  crumbLeft,
  crumbRight,
  blurb,
  children,
}: {
  /** Section-rule left crumb, e.g. "// RECOVER ACCOUNT" */
  crumbLeft: string;
  /** Section-rule right crumb (muted), e.g. "issue reset token" */
  crumbRight: string;
  /** One-line description under the section-rule. */
  blurb: ReactNode;
  /** Form area below the blurb. */
  children: ReactNode;
}) {
  return (
    <div className="grid min-h-screen bg-[var(--color-background)] text-[var(--color-foreground)] lg:grid-cols-[1.1fr_1fr]">
      {/* ─── Left pane — brand stage ───────────────────────────────── */}
      <aside className="relative hidden overflow-hidden border-r border-[var(--color-border)] lg:flex lg:flex-col">
        <div className="canvas-mesh pointer-events-none absolute inset-0" aria-hidden />
        <div
          className="pointer-events-none absolute inset-0"
          aria-hidden
          style={{
            background:
              "radial-gradient(48rem 32rem at 0% 0%, oklch(from var(--color-accent-signal) l c h / 0.18), transparent 65%)",
          }}
        />
        <CornerTicks />
        <div className="relative flex flex-1 flex-col justify-between p-12 xl:p-16">
          <div className="meta text-[var(--color-muted-foreground)] fsh-enter">
            // FSH / CONSOLE / RECOVER
          </div>
          <BrandMarkXL className="fsh-enter fsh-enter-2 max-w-lg" />
          <div className="fsh-enter fsh-enter-4 flex items-end justify-between gap-6">
            <div className="space-y-1">
              <div className="meta text-[var(--color-muted-foreground)]">authorized personnel</div>
              <div className="font-mono text-[12px] text-[var(--color-muted-foreground)] leading-relaxed">
                Account recovery is rate-limited and audited.
                <br />
                Reset links expire 30 minutes after issue.
              </div>
            </div>
            <div className="meta text-right text-[var(--color-muted-foreground)]">
              v0.1
              <br />
              build · live
            </div>
          </div>
        </div>
      </aside>

      {/* ─── Right pane — page content ─────────────────────────────── */}
      <main className="relative flex flex-col items-center justify-center p-6 lg:p-10">
        <div className="canvas-grid pointer-events-none absolute inset-0" aria-hidden />

        <div className="relative w-full max-w-md space-y-6 fsh-enter">
          {/* Mobile-only brand (lg+ uses the left pane). */}
          <div className="lg:hidden">
            <BrandMarkXL />
          </div>

          <div className="space-y-3">
            <div className="section-rule">
              <span className="section-rule__crumb">{crumbLeft}</span>
              <span className="section-rule__crumb section-rule__crumb--muted">{crumbRight}</span>
            </div>
            <p className="text-sm text-[var(--color-muted-foreground)]">{blurb}</p>
          </div>

          {children}
        </div>
      </main>
    </div>
  );
}

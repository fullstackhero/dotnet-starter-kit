import { useEffect, type ReactNode } from "react";
import { ShieldCheck } from "lucide-react";
import { cn } from "@/lib/cn";

// ────────────────────────────────────────────────────────────────────────
// AuthShell — shared atmospheric chrome for unauthenticated pages
// (login, forgot-password, reset-password, confirm-email).
//
// Mirrors the editorial-console aesthetic established in login.tsx
// (parallax orbs, dot-grid overlay, blueprint corner brackets, mono
// uppercase eyebrows) but extracted so the secondary auth pages don't
// each re-implement ~120 lines of chrome. Login.tsx itself stays inline
// for now — it carries page-specific content (tech marquee, demo panel,
// stat strip) that doesn't generalise.
//
// Usage:
//   <AuthShell eyebrow="// 02.RESET-PASSWORD" tagline="security · tenant">
//     <AuthHeadline lead="Set a new" accent="password" trail="for your account." />
//     <p>...</p>
//     <form>...</form>
//   </AuthShell>
// ────────────────────────────────────────────────────────────────────────

function useViewportParallax() {
  useEffect(() => {
    let rafId = 0;
    const onMove = (e: MouseEvent) => {
      if (rafId) return;
      rafId = requestAnimationFrame(() => {
        const xn = (e.clientX / window.innerWidth - 0.5) * 2;
        const yn = (e.clientY / window.innerHeight - 0.5) * 2;
        document.documentElement.style.setProperty("--px", String(xn));
        document.documentElement.style.setProperty("--py", String(yn));
        rafId = 0;
      });
    };
    window.addEventListener("mousemove", onMove, { passive: true });
    return () => {
      window.removeEventListener("mousemove", onMove);
      if (rafId) cancelAnimationFrame(rafId);
      document.documentElement.style.removeProperty("--px");
      document.documentElement.style.removeProperty("--py");
    };
  }, []);
}

function TopStrip() {
  return (
    <div className="relative z-10 flex items-center justify-between px-6 pt-6 sm:px-10 sm:pt-8">
      <div className="flex items-center gap-2.5">
        <span
          aria-hidden
          className={cn(
            "brand-mark grid h-8 w-8 place-items-center rounded-md",
            "text-[12px] font-bold tracking-tight text-[var(--color-primary-foreground)]",
            "shadow-[0_1px_0_oklch(1_0_0_/_0.20)_inset,0_8px_22px_-8px_oklch(from_var(--color-primary)_l_c_h_/_0.60)]",
          )}
        >
          F
        </span>
        <span className="hidden font-semibold tracking-tight sm:inline">fullstackhero</span>
      </div>

      <div className="flex items-center gap-2 font-mono text-[10.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
        <span
          aria-hidden
          className="pulse-dot inline-block h-1.5 w-1.5 rounded-full"
          style={{
            backgroundColor: "var(--color-success)",
            color: "var(--color-success)",
          }}
        />
        <span>Service ready</span>
        <span aria-hidden className="mx-1 h-3 w-px bg-[var(--color-border-strong)]" />
        <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5">v0.1</code>
      </div>
    </div>
  );
}

function CornerBrackets() {
  const base = "absolute h-3 w-3 border-[var(--color-primary)] opacity-80";
  return (
    <>
      <span aria-hidden className={cn(base, "-top-px -left-px border-l border-t")} />
      <span aria-hidden className={cn(base, "-top-px -right-px border-r border-t")} />
      <span aria-hidden className={cn(base, "-bottom-px -left-px border-l border-b")} />
      <span aria-hidden className={cn(base, "-bottom-px -right-px border-r border-b")} />
    </>
  );
}

/**
 * Floating-label input that mirrors login.tsx's FloatField. CSS-only —
 * the label position is driven by :placeholder-shown so there's no state
 * to coordinate. Use `placeholder=" "` to keep it as a no-op visual.
 */
export function FloatField({
  id,
  label,
  ...inputProps
}: {
  id: string;
  label: string;
} & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div className="float-field">
      <input id={id} placeholder=" " {...inputProps} />
      <label htmlFor={id}>{label}</label>
    </div>
  );
}

/**
 * Display headline with a gradient on the accent word. Matches the
 * "Welcome back." pattern used in login.tsx — `lead` is plain text,
 * `accent` is the gradient noun, `trail` is the closing punctuation /
 * remaining copy. All three are optional so consumers can compose.
 */
export function AuthHeadline({
  lead,
  accent,
  trail,
}: {
  lead?: string;
  accent?: string;
  trail?: string;
}) {
  return (
    <h2 className="text-display pb-1 text-[22px] font-semibold leading-[1.1] tracking-[-0.018em]">
      {lead && <>{lead} </>}
      {accent && <span className="text-gradient-brand">{accent}</span>}
      {trail && <>{trail}</>}
    </h2>
  );
}

export function AuthShell({
  eyebrow,
  tagline,
  children,
  footer,
}: {
  /** Mono uppercase tag at the top of the card, e.g. "// 02.RESET-PASSWORD" */
  eyebrow: string;
  /** Right-aligned mono caption, e.g. "tenant · token" */
  tagline?: string;
  /** Card body */
  children: ReactNode;
  /** Optional row beneath the card — e.g. "Back to sign in" link */
  footer?: ReactNode;
}) {
  useViewportParallax();

  return (
    <div className="relative flex min-h-screen flex-col overflow-hidden">
      {/* Atmospheric background — three drifting orbs that follow the
          cursor, behind a low-opacity dot-grid that reads as graph paper. */}
      <div
        aria-hidden
        className="parallax-orb pointer-events-none absolute -left-32 -top-40 h-[640px] w-[640px] rounded-full blur-[160px]"
        style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.30)" }}
      />
      <div
        aria-hidden
        className="parallax-orb-2 pointer-events-none absolute -right-40 top-1/3 h-[680px] w-[680px] rounded-full blur-[180px]"
        style={{ backgroundColor: "oklch(0.700 0.155 195 / 0.22)" }}
      />
      <div
        aria-hidden
        className="parallax-orb pointer-events-none absolute left-1/3 -bottom-40 h-[560px] w-[560px] rounded-full blur-[160px]"
        style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.16)" }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-[0.65]"
        style={{
          backgroundImage:
            "radial-gradient(circle, oklch(from var(--color-foreground) l c h / 0.10) 1px, transparent 1px)",
          backgroundSize: "32px 32px",
          maskImage:
            "radial-gradient(ellipse 70% 60% at 50% 50%, black 30%, transparent 75%)",
          WebkitMaskImage:
            "radial-gradient(ellipse 70% 60% at 50% 50%, black 30%, transparent 75%)",
        }}
      />

      <TopStrip />

      <main className="relative z-10 flex flex-1 flex-col items-center justify-center px-6 py-10 sm:px-10">
        <div className="mx-auto flex w-full max-w-[440px] flex-col">
          <div className="fsh-enter fsh-enter-4 relative">
            <CornerBrackets />
            <div
              className={cn(
                "relative rounded-2xl",
                "bg-[oklch(from_var(--color-card)_l_c_h_/_0.78)] backdrop-blur-2xl backdrop-saturate-150",
                "border border-[var(--color-border)]",
                "px-7 pb-6 pt-6 text-left",
                "shadow-[0_30px_60px_-30px_oklch(0_0_0_/_0.30),0_12px_24px_-16px_oklch(0_0_0_/_0.20)]",
              )}
            >
              <div className="mb-5 flex items-center justify-between gap-2 font-mono text-[10.5px] font-medium uppercase tracking-[0.18em]">
                <span className="text-[var(--color-primary)]">{eyebrow}</span>
                {tagline && (
                  <span className="text-[var(--color-muted-foreground)]">{tagline}</span>
                )}
              </div>
              {children}
            </div>
          </div>

          {footer && (
            <div className="fsh-enter fsh-enter-5 mt-6 text-center text-[12.5px] text-[var(--color-muted-foreground)]">
              {footer}
            </div>
          )}

          <div className="fsh-enter fsh-enter-5 mt-7 inline-flex items-center justify-center gap-1.5 self-center text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
            <ShieldCheck className="h-3 w-3" />
            <span>Encrypted in transit · JWT-secured session</span>
          </div>
        </div>
      </main>

      <footer className="relative z-10 px-6 py-5 text-center text-xs text-[var(--color-muted-foreground)]">
        <span>
          Need a tenant?{" "}
          <a
            href="https://fullstackhero.net"
            target="_blank"
            rel="noreferrer"
            className="text-[var(--color-foreground)] underline-offset-4 hover:underline"
          >
            Get started
          </a>
        </span>
        <span className="mx-3 text-[var(--color-border-strong)]">·</span>
        <span className="font-mono">v0.1 · console</span>
      </footer>
    </div>
  );
}

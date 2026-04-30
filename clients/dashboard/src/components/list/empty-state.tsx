import type { ReactNode } from "react";
import { Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

type Action = {
  label: string;
  onClick: () => void;
  icon?: ReactNode;
};

/**
 * EmptyState — the "plinth" treatment for zero-result list pages.
 *
 * Visual concept: a museum/showroom plinth where the missing item would sit.
 * A slow conic halo orbits behind the icon (like a slow-pacing spotlight),
 * a soft floor gradient rises from below the plinth, and a single cast
 * shadow grounds the icon. Designed to match the rest of the dashboard's
 * atmospheric vocabulary — gradient-border surfaces, mono-caps eyebrows,
 * display headlines.
 *
 * Two action slots: a primary CTA (always shown — usually "create the first
 * X" or "add a new X") and an optional secondary (clear filters / search).
 */
export function EmptyState({
  eyebrow,
  headline,
  body,
  icon,
  primaryAction,
  secondaryAction,
}: {
  eyebrow: string;
  headline: ReactNode;
  body: ReactNode;
  icon: ReactNode;
  primaryAction: Action;
  secondaryAction?: Action;
}) {
  return (
    <div className="relative isolate overflow-hidden">
      {/* Floor — soft radial that rises from beneath the plinth, suggesting
          a vanishing horizon. Layered above the cast shadow so the shadow
          reads as depth, not haze. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage: `
            radial-gradient(50% 60% at 50% 38%, oklch(from var(--color-primary) l c h / 0.10), transparent 70%),
            radial-gradient(60% 40% at 50% 100%, oklch(from var(--color-primary) l c h / 0.05), transparent 70%)
          `,
        }}
      />
      {/* Grain — barely-there texture so the gradient doesn't band. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.05] mix-blend-overlay"
        style={{
          backgroundImage:
            "url(\"data:image/svg+xml;utf8,<svg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='2' stitchTiles='stitch'/></filter><rect width='100%' height='100%' filter='url(%23n)'/></svg>\")",
        }}
      />

      <div className="relative flex flex-col items-center gap-5 px-6 py-16 text-center sm:py-20">
        {/* Plinth — icon with orbiting halo + cast shadow */}
        <Plinth>{icon}</Plinth>

        {/* Eyebrow */}
        <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
          {eyebrow}
        </span>

        {/* Display headline — generous max-width keeps two-line wraps from
            looking awkward; tracking tightens for that quiet, confident feel */}
        <h3 className="text-display max-w-md text-xl font-semibold leading-[1.2] tracking-[-0.02em] text-balance sm:text-[22px]">
          {headline}
        </h3>

        <p className="max-w-md text-sm leading-relaxed text-[var(--color-muted-foreground)] text-balance">
          {body}
        </p>

        <div className="mt-2 flex flex-wrap items-center justify-center gap-2">
          {secondaryAction && (
            <Button
              variant="outline"
              size="sm"
              onClick={secondaryAction.onClick}
              className="gap-1.5"
            >
              {secondaryAction.icon}
              {secondaryAction.label}
            </Button>
          )}
          <Button
            onClick={primaryAction.onClick}
            className="brand-glow gradient-sheen gap-1.5"
          >
            {primaryAction.icon ?? <Sparkles className="h-3.5 w-3.5" />}
            {primaryAction.label}
          </Button>
        </div>
      </div>
    </div>
  );
}

/**
 * Plinth — the visual anchor. Three concentric layers from back to front:
 *
 *   1. Cast shadow — a soft elliptical radial gradient beneath, grounding
 *      the icon visually so it doesn't float like a sticker.
 *   2. Conic halo — a slow-rotating dual-stop conic gradient masked into
 *      a ring. Reads as a single bright wedge orbiting the icon over 18s.
 *   3. Icon plate — gradient-bordered, soft brand-tinted fill, on top.
 */
function Plinth({ children }: { children: ReactNode }) {
  return (
    <div className="relative grid h-20 w-20 place-items-center">
      {/* Cast shadow */}
      <span
        aria-hidden
        className="absolute -bottom-3 left-1/2 h-3 w-16 -translate-x-1/2 rounded-full"
        style={{
          background:
            "radial-gradient(ellipse 100% 100% at 50% 50%, oklch(from var(--color-primary) l c h / 0.30), transparent 70%)",
          filter: "blur(6px)",
        }}
      />

      {/* Orbiting halo — masked conic ring */}
      <span
        aria-hidden
        className={cn(
          "absolute inset-[-6px] rounded-[28px]",
          "[animation:fsh-spin_18s_linear_infinite]",
        )}
        style={{
          background:
            "conic-gradient(from 0deg, transparent 0deg, oklch(from var(--color-primary) l c h / 0.55) 60deg, transparent 120deg, transparent 360deg)",
          // Mask to a 1.5px ring so we only see the orbiting wedge as a glow
          // tracing the perimeter, not a filled disc.
          WebkitMask:
            "radial-gradient(circle, transparent 60%, black 62%, black 70%, transparent 72%)",
          mask: "radial-gradient(circle, transparent 60%, black 62%, black 70%, transparent 72%)",
        }}
      />

      {/* Static base ring for a constant outline so the orbit reads as a
          highlight rather than the only chrome */}
      <span
        aria-hidden
        className="absolute inset-[-2px] rounded-2xl ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.20)]"
      />

      {/* Icon plate */}
      <span
        aria-hidden
        className={cn(
          "relative z-10 grid h-14 w-14 place-items-center rounded-2xl",
          "bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.20),oklch(from_var(--color-primary)_l_c_h_/_0.04))]",
          "ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.28)]",
          "shadow-[0_8px_22px_-12px_oklch(from_var(--color-primary)_l_c_h_/_0.55)]",
        )}
      >
        {children}
      </span>
    </div>
  );
}

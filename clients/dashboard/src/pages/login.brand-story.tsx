import { useEffect, useState } from "react";
import { cn } from "@/lib/cn";

// ────────────────────────────────────────────────────────────────────────
// Editorial hero panel for the login page. Strict typographic hierarchy:
//   eyebrow → massive headline → tagline crossfade → editorial stat strip.
// No feature pills, no decorative chrome — the page leans on type and
// negative space. Demo panel + form panel sit beside this column on
// wide viewports.
// ────────────────────────────────────────────────────────────────────────

const TAGLINES: ReadonlyArray<string> = [
  "Production-grade .NET 10 starter kit.",
  "Modular monolith, ready to ship.",
  "Multi-tenant from day one.",
  "Aspire-orchestrated. SSE-powered.",
] as const;

const TAGLINE_INTERVAL_MS = 4200;

type Stat = { value: string; label: string };

const STATS: ReadonlyArray<Stat> = [
  { value: "14", label: "Modules" },
  { value: "08", label: "Building blocks" },
  { value: "02", label: "Demo apps" },
];

export function LoginBrandStory() {
  const [tagIdx, setTagIdx] = useState(0);

  useEffect(() => {
    const id = window.setInterval(() => {
      setTagIdx((i) => (i + 1) % TAGLINES.length);
    }, TAGLINE_INTERVAL_MS);
    return () => window.clearInterval(id);
  }, []);

  return (
    <aside
      aria-hidden
      className="fsh-enter fsh-enter-1 relative z-10 flex flex-col justify-between gap-10 py-2"
    >
      {/* Eyebrow + Headline + Tagline */}
      <div>
        <div className="flex items-center gap-2.5 font-mono text-[10.5px] font-medium uppercase tracking-[0.20em] text-[var(--color-muted-foreground)]">
          <span className="inline-block h-px w-8 bg-[var(--color-border-strong)]" aria-hidden />
          <span>The starter kit</span>
        </div>

        {/* Massive editorial headline. Fluid clamp scales from 40px on
            narrow lg viewports to 80px on very wide ones. The display
            font is the user's currently-selected sans (controlled via
            the appearance theme). pb-2 protects descenders from being
            clipped against the headline's own line-box at tight
            line-heights. */}
        <h2
          className="text-display mt-4 pb-2 font-semibold leading-[1.02] tracking-[-0.028em]"
          style={{ fontSize: "clamp(2.5rem, 1.5rem + 4vw, 5rem)" }}
        >
          The complete{" "}
          <span className="text-gradient-brand">.NET 10</span>
          <br />
          starter kit your team
          <br />
          has been waiting for.
        </h2>

        {/* Tagline cycle — fixed-height container so the crossfade
            doesn't reflow neighbouring content. */}
        <div
          className="relative mt-6 h-7 max-w-md overflow-hidden"
          aria-live="polite"
        >
          {TAGLINES.map((line, i) => (
            <p
              key={line}
              className={cn(
                "absolute inset-0 text-[15px] leading-relaxed text-[var(--color-muted-foreground)]",
                "transition-opacity duration-[700ms] ease-[var(--ease-out-cubic)]",
                i === tagIdx ? "opacity-100" : "opacity-0",
              )}
            >
              {line}
            </p>
          ))}
        </div>
      </div>

      {/* Editorial stat strip — three numbers, mono caps captions, hairline
          dividers. No box chrome — pure typography sitting on the page. */}
      <div className="flex items-stretch gap-8">
        {STATS.map((stat, i) => (
          <div
            key={stat.label}
            className={cn(
              "fsh-enter relative",
              i > 0 && "border-l border-[var(--color-border-strong)] pl-8",
            )}
            style={{ animationDelay: `${160 + i * 80}ms` }}
          >
            <div className="text-display text-[40px] font-semibold leading-none tabular-nums tracking-[-0.025em]">
              {stat.value}
            </div>
            <div className="mt-1.5 font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              {stat.label}
            </div>
          </div>
        ))}
      </div>
    </aside>
  );
}

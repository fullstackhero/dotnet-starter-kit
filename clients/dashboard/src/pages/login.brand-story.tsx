import { useEffect, useState } from "react";
import {
  Activity,
  Database,
  KeyRound,
  Layers,
} from "lucide-react";
import { cn } from "@/lib/cn";

// ────────────────────────────────────────────────────────────────────────
// Tagline cycle — soft crossfade through a few short statements about
// what the product is. Phrases stay on screen for ~4s each. Pure CSS
// transition driven by a setInterval that bumps an index; the previous
// line fades out as the new one fades in.
// ────────────────────────────────────────────────────────────────────────

const TAGLINES: ReadonlyArray<string> = [
  "Production-grade .NET 10 starter kit.",
  "Modular monolith, ready to ship.",
  "Multi-tenant from day one.",
  "Aspire-orchestrated. SSE-powered.",
] as const;

const TAGLINE_INTERVAL_MS = 4000;

type TrustItem = {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  description: string;
};

const TRUST_ITEMS: ReadonlyArray<TrustItem> = [
  { icon: KeyRound, label: "JWT", description: "Bearer-secured sessions, refresh-rotated" },
  { icon: Layers, label: "Modular", description: "VSA + Mediator handlers per slice" },
  { icon: Database, label: "EF Core 10", description: "PostgreSQL · Finbuckle multi-tenancy" },
  { icon: Activity, label: "Live SSE", description: "Server-Sent Events stream into the UI" },
];

/**
 * Marketing-style left column shown beside the login card on wide
 * viewports. Stays flat on narrow viewports — caller decides via
 * grid template whether this renders.
 */
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
      className={cn(
        "fsh-enter fsh-enter-1 relative z-10 flex flex-col justify-between",
        "min-h-[28rem] gap-10 px-2 py-2 lg:px-4",
      )}
    >
      {/* Wordmark + tagline cycle ───────────────────────────────────── */}
      <div>
        <div className="flex items-center gap-3">
          <span
            aria-hidden
            className={cn(
              "brand-mark grid h-10 w-10 place-items-center rounded-lg",
              "text-[16px] font-bold tracking-tight text-[var(--color-primary-foreground)]",
              "shadow-[0_1px_0_oklch(1_0_0_/_0.20)_inset,0_8px_22px_-8px_oklch(from_var(--color-primary)_l_c_h_/_0.65)]",
            )}
          >
            F
          </span>
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            FullStackHero · console
          </span>
        </div>

        {/* Headline — display serif, gradient on the noun. */}
        <h2
          className="text-display mt-7 font-semibold leading-[1.04] tracking-[-0.022em]"
          style={{ fontSize: "clamp(2rem, 1.4rem + 2.4vw, 2.875rem)" }}
        >
          The starter kit your{" "}
          <span className="text-gradient-brand">.NET</span>{" "}
          team has been waiting for.
        </h2>

        {/* Tagline cycle — fixed-height container so the rotating
            crossfade doesn't reflow neighbouring content.  */}
        <div
          className="mt-5 relative h-6 max-w-md overflow-hidden"
          aria-live="polite"
        >
          {TAGLINES.map((line, i) => (
            <p
              key={line}
              className={cn(
                "absolute inset-0 text-[15px] leading-relaxed text-[var(--color-muted-foreground)]",
                "transition-opacity duration-[600ms] ease-[var(--ease-out-cubic)]",
                i === tagIdx ? "opacity-100" : "opacity-0",
              )}
            >
              {line}
            </p>
          ))}
        </div>
      </div>

      {/* Trust strip — feature pills with icon + label + tooltip-like
          description. Stacked stagger entrance via fsh-enter. */}
      <div className="space-y-2.5">
        <div className="flex items-center gap-2 font-mono text-[10.5px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
          <span
            aria-hidden
            className="pulse-dot inline-block h-1.5 w-1.5 rounded-full"
            style={{ backgroundColor: "var(--color-success)", color: "var(--color-success)" }}
          />
          Service ready
        </div>
        <ul className="grid grid-cols-2 gap-2">
          {TRUST_ITEMS.map((item, i) => (
            <li
              key={item.label}
              className="fsh-enter"
              style={{ animationDelay: `${120 + i * 60}ms` }}
            >
              <TrustPill item={item} />
            </li>
          ))}
        </ul>
      </div>
    </aside>
  );
}

function TrustPill({ item }: { item: TrustItem }) {
  const Icon = item.icon;
  return (
    <div
      className={cn(
        "group/trust relative flex h-full items-start gap-2.5 rounded-xl border bg-[oklch(from_var(--color-card)_l_c_h_/_0.55)] px-3 py-2.5",
        "border-[var(--color-border)] backdrop-blur-md",
        "transition-colors duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
        "hover:border-[var(--color-border-strong)]",
      )}
    >
      <span
        aria-hidden
        className="grid h-7 w-7 shrink-0 place-items-center rounded-md ring-1 ring-inset"
        style={{
          background:
            "linear-gradient(135deg, oklch(from var(--color-primary) l c h / 0.20), oklch(from var(--color-primary) l c h / 0.02))",
          color: "var(--color-primary)",
          boxShadow: "inset 0 0 0 1px oklch(from var(--color-primary) l c h / 0.25)",
        }}
      >
        <Icon className="h-3.5 w-3.5" />
      </span>
      <div className="min-w-0 flex-1">
        <div className="truncate text-[12px] font-semibold tracking-tight">
          {item.label}
        </div>
        <div className="mt-0.5 text-[11px] leading-relaxed text-[var(--color-muted-foreground)]">
          {item.description}
        </div>
      </div>
    </div>
  );
}


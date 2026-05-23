import { cn } from "@/lib/cn";

/**
 * BrandMark — the Console wordmark. Two glyphs side-by-side:
 *   • A small chartreuse square "punctuation" mark (the only place chrome
 *     uses the accent at full saturation).
 *   • A mono "FSH" lockup with tight letter-spacing.
 * Designed to feel like a system header line rather than a logo.
 */
export function BrandMark({ className }: { className?: string }) {
  return (
    <div className={cn("inline-flex items-center gap-2 select-none", className)}>
      <span
        aria-hidden
        className="block h-2.5 w-2.5 rounded-[2px] bg-[var(--color-accent-signal)] shadow-[0_0_12px_oklch(from_var(--color-accent-signal)_l_c_h_/_0.45)]"
      />
      <span className="font-mono text-[13px] font-semibold tracking-[0.16em] uppercase text-[var(--color-foreground)]">
        FSH
        <span className="text-[var(--color-muted-foreground)]">/admin</span>
      </span>
    </div>
  );
}

/**
 * BrandMarkXL — splash version for the Login page. Leads with the FSH logo
 * mark + "fullstackhero" wordmark, then the "Console." display monogram and
 * a one-line system blurb. The chartreuse signal carries through the wordmark
 * accent and the monogram period.
 */
export function BrandMarkXL({ className }: { className?: string }) {
  return (
    <div className={cn("space-y-3", className)}>
      <div className="flex items-center gap-2.5">
        <img
          src="/logo-fullstackhero.png"
          alt="FullStackHero"
          className="size-7 object-contain"
        />
        <span className="font-display text-[18px] font-semibold tracking-tight text-[var(--color-foreground)]">
          fullstack<span className="text-[var(--color-accent-signal)]">hero</span>
        </span>
        <span className="meta text-[var(--color-muted-foreground)]">· platform admin</span>
      </div>
      <h1 className="font-display text-[clamp(3rem,7vw,5.5rem)] font-semibold leading-[0.95] tracking-[var(--tracking-display)]">
        Console<span className="text-[var(--color-accent-signal)]">.</span>
      </h1>
      <p className="max-w-md text-sm text-[var(--color-muted-foreground)] leading-relaxed">
        Operate every tenant on this instance — identity, multitenancy, billing,
        and the rest of the system surface, from one place.
      </p>
    </div>
  );
}

import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";

export function NotFoundPage() {
  return (
    <div className="relative flex min-h-screen items-center justify-center bg-[var(--color-background)] text-[var(--color-foreground)] p-6">
      <div className="canvas-mesh pointer-events-none absolute inset-0" aria-hidden />
      <div
        className="pointer-events-none absolute inset-0"
        aria-hidden
        style={{
          background:
            "radial-gradient(36rem 24rem at 50% 110%, oklch(from var(--color-accent-signal) l c h / 0.12), transparent 70%)",
        }}
      />
      <div className="fsh-enter relative max-w-md space-y-6 text-center">
        <div className="meta text-[var(--color-muted-foreground)]">
          // SYSTEM RESPONSE
        </div>
        <h1 className="font-display text-[clamp(5rem,12vw,8rem)] font-semibold leading-[0.95] tracking-[var(--tracking-display)]">
          404<span className="text-[var(--color-accent-signal)]">.</span>
        </h1>
        <p className="font-mono text-sm text-[var(--color-muted-foreground)]">
          requested resource &nbsp;<span className="code-chip">not_found</span>
        </p>
        <p className="text-sm text-[var(--color-foreground)] leading-relaxed">
          The URL you requested doesn't map to any page in this console.
        </p>
        <Button asChild variant="signal">
          <Link to="/">Return to overview →</Link>
        </Button>
      </div>
    </div>
  );
}

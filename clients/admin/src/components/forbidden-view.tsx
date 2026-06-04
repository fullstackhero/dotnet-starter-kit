import { Link } from "react-router-dom";
import { ShieldOff } from "lucide-react";
import { Button } from "@/components/ui/button";

type ForbiddenViewProps = {
  /** Permission strings the caller required that the user doesn't hold. Shown for operator clarity. */
  missing?: string[];
};

/**
 * ForbiddenView — 403 surface in the Console language. Hairline-bordered
 * mono crumb, single chartreuse accent rule, missing permissions printed
 * as code-chips so the operator can paste them into a permission grant.
 */
export function ForbiddenView({ missing }: ForbiddenViewProps) {
  return (
    <div className="flex min-h-[60vh] items-center justify-center p-6">
      <div className="card-shell w-full max-w-md px-8 py-9">
        <div className="flex items-center gap-3">
          <span className="grid h-10 w-10 place-items-center rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)]">
            <ShieldOff className="h-5 w-5" />
          </span>
          <div className="meta text-[var(--color-muted-foreground)]">
            // 403 · access denied
          </div>
        </div>

        <h2 className="mt-5 font-display text-2xl font-semibold tracking-tight">
          You don&apos;t hold the permissions to view this surface.
        </h2>
        <p className="mt-2 text-sm text-[var(--color-muted-foreground)] leading-relaxed">
          Ask a root-tenant operator to grant the permissions below to a role you hold.
        </p>

        {missing && missing.length > 0 && (
          <div className="mt-5 space-y-1.5 border-l-2 border-[var(--color-accent-signal)] pl-3">
            <div className="meta text-[var(--color-muted-foreground)]">missing</div>
            <ul className="flex flex-wrap gap-1.5">
              {missing.map((p) => (
                <li key={p}>
                  <code className="code-chip">{p}</code>
                </li>
              ))}
            </ul>
          </div>
        )}

        <div className="mt-7 flex items-center gap-2">
          <Button asChild variant="outline" size="sm">
            <Link to="/">← Overview</Link>
          </Button>
        </div>
      </div>
    </div>
  );
}

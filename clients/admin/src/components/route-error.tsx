import { isRouteErrorResponse, useNavigate, useRouteError } from "react-router-dom";
import { Button } from "@/components/ui/button";

/**
 * RouteError — Console-styled error boundary. React Router passes any error
 * thrown during render, loading, or actions to the nearest
 * <Route errorElement>. We surface it with the same editorial 4xx/5xx
 * treatment the NotFound page uses, plus a collapsible stack dump for the
 * rare cases an operator needs it.
 */
export function RouteError() {
  const error = useRouteError();
  const navigate = useNavigate();
  const { status, title, detail } = describe(error);

  return (
    <div className="relative flex min-h-screen items-center justify-center bg-[var(--color-background)] p-6">
      <div className="canvas-mesh pointer-events-none absolute inset-0" aria-hidden />
      <div
        className="pointer-events-none absolute inset-0"
        aria-hidden
        style={{
          background:
            "radial-gradient(36rem 24rem at 50% 110%, oklch(from var(--color-destructive) l c h / 0.10), transparent 70%)",
        }}
      />

      <div className="fsh-enter relative w-full max-w-xl space-y-6 text-center">
        <div className="meta text-[var(--color-muted-foreground)]">// SYSTEM RESPONSE</div>

        <h1 className="font-display text-[clamp(4rem,9vw,7rem)] font-semibold leading-[0.95] tracking-[var(--tracking-display)]">
          {status}
          <span className="text-[var(--color-destructive)]">.</span>
        </h1>

        <p className="font-mono text-sm text-[var(--color-muted-foreground)]">
          <span className="code-chip text-[var(--color-destructive)]">{title}</span>
        </p>

        {detail && (
          <details className="group mx-auto w-full max-w-lg text-left">
            <summary className="meta cursor-pointer text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]">
              <span className="select-none">// stack trace · click to expand</span>
            </summary>
            <pre className="mt-3 max-h-60 overflow-auto rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-3 text-left font-mono text-[11px] leading-relaxed text-[var(--color-muted-foreground)] whitespace-pre-wrap">
              {detail}
            </pre>
          </details>
        )}

        <div className="flex items-center justify-center gap-2 pt-2">
          <Button variant="signal" onClick={() => navigate(0)}>
            Reload →
          </Button>
          <Button variant="outline" onClick={() => navigate("/")}>
            Return to overview
          </Button>
        </div>
      </div>
    </div>
  );
}

function describe(error: unknown): { status: string; title: string; detail?: string } {
  if (isRouteErrorResponse(error)) {
    return {
      status: String(error.status),
      title: error.statusText || "Route error",
      detail: typeof error.data === "string" ? error.data : safeStringify(error.data),
    };
  }
  if (error instanceof Error) {
    return { status: "5XX", title: error.message, detail: error.stack };
  }
  return { status: "5XX", title: "Unexpected error", detail: safeStringify(error) };
}

function safeStringify(value: unknown): string | undefined {
  if (value === undefined || value === null) return undefined;
  try {
    return JSON.stringify(value, null, 2);
  } catch {
    return String(value);
  }
}

import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { AlertCircle, ArrowRight, CheckCircle2, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { AuthHeadline, AuthShell } from "@/components/auth/auth-shell";
import { confirmEmail } from "@/api/identity";
import { ApiRequestError } from "@/lib/api-client";

/**
 * Confirm-email landing — the link sent during registration brings the
 * user here with `?userId=&code=&tenant=` query parameters. We fire the
 * GET /confirm-email call once on mount and surface a stable success or
 * failure state — no form, no manual action required.
 *
 * Three states: in-flight (spinner), success (green check + go-to-login),
 * failure (red x + "request a new link" affordance). We intentionally
 * don't auto-redirect on success — the user just clicked an email link,
 * they want to see the outcome before the next click.
 */

type Status =
  | { kind: "loading" }
  | { kind: "success"; message: string }
  | { kind: "error"; message: string };

export function ConfirmEmailPage() {
  const [params] = useSearchParams();
  const userId = params.get("userId") ?? "";
  const code = params.get("code") ?? "";
  const tenant = params.get("tenant") ?? "";
  const malformed = !userId || !code || !tenant;

  const [status, setStatus] = useState<Status>({ kind: "loading" });

  // We run the call exactly once per mount keyed on the URL params —
  // double-firing the GET in StrictMode would still be idempotent
  // server-side (UserManager.ConfirmEmailAsync is idempotent for the
  // same token), but holding off the second call keeps the UI honest.
  useEffect(() => {
    if (malformed) {
      setStatus({
        kind: "error",
        message:
          "This confirmation link is missing required parameters. It may have been clipped by your email client.",
      });
      return;
    }

    let cancelled = false;
    void confirmEmail({ userId, code, tenant })
      .then((message) => {
        if (cancelled) return;
        setStatus({
          kind: "success",
          message:
            typeof message === "string" && message.length > 0
              ? message
              : "Your email is confirmed. You can now sign in.",
        });
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        const detail =
          err instanceof ApiRequestError
            ? err.problem?.detail ?? err.problem?.title ?? err.message
            : (err as Error).message;
        setStatus({ kind: "error", message: detail });
      });

    return () => {
      cancelled = true;
    };
  }, [userId, code, tenant, malformed]);

  return (
    <AuthShell
      footer={
        <Link
          to="/login"
          className="text-[var(--color-foreground)] underline-offset-4 hover:underline"
        >
          ← Back to sign in
        </Link>
      }
    >
      {status.kind === "loading" && (
        <div className="space-y-5 text-center">
          <div className="grid place-items-center">
            <span
              aria-hidden
              className="grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
            >
              <Loader2 className="size-6 animate-spin" />
            </span>
          </div>
          <div>
            <AuthHeadline lead="Verifying your" accent="email…" />
            <p className="text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
              One moment — checking the confirmation token with the server.
            </p>
          </div>
        </div>
      )}

      {status.kind === "success" && (
        <div className="fsh-enter space-y-5 text-center">
          <div className="grid place-items-center">
            <span
              aria-hidden
              className="grid size-14 place-items-center rounded-2xl bg-[oklch(from_var(--color-success)_l_c_h_/_0.10)] text-[var(--color-success)]"
            >
              <CheckCircle2 className="size-6" />
            </span>
          </div>
          <div>
            <AuthHeadline lead="Email" accent="confirmed" />
            <p className="text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
              {status.message}
            </p>
          </div>
          <Link to="/login" className="block">
            <Button type="button" className="group h-11 w-full text-[14px] font-semibold">
              <span>Continue to sign in</span>
              <ArrowRight className="size-[14px] opacity-60 transition-all duration-200 group-hover:translate-x-0.5 group-hover:opacity-100" />
            </Button>
          </Link>
        </div>
      )}

      {status.kind === "error" && (
        <div className="fsh-enter space-y-5 text-center">
          <div className="grid place-items-center">
            <span
              aria-hidden
              className="grid size-14 place-items-center rounded-2xl bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] text-[var(--color-destructive)]"
            >
              <AlertCircle className="size-6" />
            </span>
          </div>
          <div>
            <AuthHeadline lead="Couldn't" accent="confirm" trail=" your email" />
            <p className="text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
              {status.message}
            </p>
            <p className="mt-2 text-[12px] leading-relaxed text-[var(--color-muted-foreground)]">
              The link may have expired or been used already. If you've signed
              in since this email was sent, you can ignore it.
            </p>
          </div>
          <div className="flex items-center justify-center gap-2 pt-1">
            <Link to="/login">
              <Button type="button" variant="outline">
                Back to sign in
              </Button>
            </Link>
            <Link to="/forgot-password">
              <Button type="button" variant="ghost">
                Reset password instead
              </Button>
            </Link>
          </div>
        </div>
      )}
    </AuthShell>
  );
}

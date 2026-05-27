import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { AlertCircle, CheckCircle2, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { AuthShell } from "@/components/auth/auth-shell";
import { confirmEmail } from "@/api/users";
import { ApiRequestError } from "@/lib/api-client";

type Status =
  | { kind: "loading" }
  | { kind: "success"; message: string }
  | { kind: "error"; message: string };

/**
 * Confirm-email landing — admin variant.
 *
 * Same shape as the dashboard's: auto-fire GET on mount, surface stable
 * success or failure state with recovery affordances. Uses the admin's
 * editorial split-screen shell so the page reads as part of the same
 * surface as the login page.
 */
export function ConfirmEmailPage() {
  const [params] = useSearchParams();
  const userId = params.get("userId") ?? "";
  const code = params.get("code") ?? "";
  const tenant = params.get("tenant") ?? "";
  const malformed = !userId || !code || !tenant;

  const [status, setStatus] = useState<Status>({ kind: "loading" });

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
      crumbLeft="// VERIFY EMAIL"
      crumbRight={
        status.kind === "loading"
          ? "validating…"
          : status.kind === "success"
            ? "confirmed"
            : "failed"
      }
      blurb={
        status.kind === "loading"
          ? "Checking the confirmation token with the server."
          : status.kind === "success"
            ? "Your email is now verified for this tenant."
            : "We couldn't verify that confirmation token."
      }
    >
      {status.kind === "loading" && (
        <div className="space-y-4 py-2 text-center">
          <div className="grid place-items-center pt-1">
            <span
              aria-hidden
              className="grid h-12 w-12 place-items-center rounded-full border border-[var(--color-border)] bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
            >
              <Loader2 className="h-5 w-5 animate-spin" />
            </span>
          </div>
        </div>
      )}

      {status.kind === "success" && (
        <div className="space-y-5">
          <div className="grid place-items-center">
            <span
              aria-hidden
              className="grid h-12 w-12 place-items-center rounded-full border border-[oklch(from_var(--color-success)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-success)_l_c_h_/_0.10)] text-[var(--color-success)]"
            >
              <CheckCircle2 className="h-5 w-5" />
            </span>
          </div>
          <p className="text-center text-sm leading-relaxed text-[var(--color-muted-foreground)]">
            {status.message}
          </p>
          <Link to="/login" className="block">
            <Button type="button" variant="signal" className="w-full">
              Continue to sign in →
            </Button>
          </Link>
        </div>
      )}

      {status.kind === "error" && (
        <div className="space-y-5">
          <div className="grid place-items-center">
            <span
              aria-hidden
              className="grid h-12 w-12 place-items-center rounded-full border border-[var(--color-destructive)]/40 bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] text-[var(--color-destructive)]"
            >
              <AlertCircle className="h-5 w-5" />
            </span>
          </div>
          <p className="text-center text-sm leading-relaxed text-[var(--color-muted-foreground)]">
            {status.message}
          </p>
          <p className="text-center text-xs leading-relaxed text-[var(--color-muted-foreground)]">
            The link may have expired or been used already. If you've signed in since
            this email was sent, you can ignore it.
          </p>
          <div className="flex items-center gap-2">
            <Link to="/login">
              <Button type="button" variant="outline">
                Back to sign in
              </Button>
            </Link>
            <Link to="/forgot-password" className="ml-auto">
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

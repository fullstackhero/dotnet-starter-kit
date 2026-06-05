import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { AlertCircle, ArrowRight, CheckCircle2, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
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
 * success or failure state with recovery affordances. Uses the unified
 * centered-card auth shell so the page reads as part of the same surface
 * as the login and other auth pages.
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
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[var(--color-background)] px-5 py-8 sm:py-12">
      {/* Atmospheric background orbs */}
      <div className="pointer-events-none absolute inset-0" aria-hidden>
        <div
          className="absolute -top-[25%] -left-[15%] h-[70vw] w-[70vw] rounded-full blur-[140px]"
          style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.05)" }}
        />
        <div
          className="absolute -bottom-[20%] -right-[10%] h-[55vw] w-[55vw] rounded-full blur-[120px]"
          style={{ backgroundColor: "oklch(from var(--color-saffron, var(--color-primary)) l c h / 0.07)" }}
        />
        <div
          className="absolute top-[10%] right-[5%] h-[30vw] w-[30vw] rounded-full blur-[80px]"
          style={{ backgroundColor: "oklch(from var(--color-primary) l c h / 0.025)" }}
        />
      </div>

      {/* Card column */}
      <div className="relative z-10 w-full max-w-[420px] fsh-enter fsh-enter-1">
        {/* Brand lockup */}
        <div className="mb-8 flex flex-col items-center">
          <div className="flex items-center gap-2.5">
            <img
              src="/logo-fullstackhero.png"
              alt="fullstackhero"
              className="size-9 object-contain"
            />
            <span className="font-display text-[26px] font-semibold tracking-tight text-[var(--color-foreground)]">
              fullstack<span className="text-[var(--color-primary)]">hero</span>
            </span>
          </div>
          <div className="mt-3 flex items-center gap-2 text-[10px] font-semibold uppercase tracking-[0.2em] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]">
            <span aria-hidden className="h-px w-6 bg-[var(--color-border)]" />
            <span>.NET 10 Starter Kit</span>
            <span aria-hidden className="h-px w-6 bg-[var(--color-border)]" />
          </div>
        </div>

        {/* Status card */}
        <div className="rounded-xl border border-[var(--color-border)] bg-[oklch(from_var(--color-card)_l_c_h_/_0.85)] shadow-[0_1px_3px_oklch(0_0_0_/_0.04),0_8px_24px_oklch(0_0_0_/_0.06)] backdrop-blur-xl">
          <div className="px-6 py-7 sm:px-8 sm:py-9">
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
                  <h1 className="mb-1.5 font-display text-[22px] font-semibold tracking-tight text-[var(--color-foreground)]">
                    Verifying your{" "}
                    <span className="text-[var(--color-primary)]">email…</span>
                  </h1>
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
                  <h1 className="mb-1.5 font-display text-[22px] font-semibold tracking-tight text-[var(--color-foreground)]">
                    Email{" "}
                    <span className="text-[var(--color-primary)]">confirmed</span>
                  </h1>
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
                  <h1 className="mb-1.5 font-display text-[22px] font-semibold tracking-tight text-[var(--color-foreground)]">
                    Couldn't{" "}
                    <span className="text-[var(--color-primary)]">confirm</span>{" "}
                    your email
                  </h1>
                  <p className="text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
                    {status.message}
                  </p>
                  <p className="mt-2 text-[12px] leading-relaxed text-[var(--color-muted-foreground)]">
                    The link may have expired or been used already. If you've signed in since
                    this email was sent, you can ignore it.
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
          </div>
        </div>

        <div className="mt-6 text-center">
          <Link
            to="/login"
            className="text-[12.5px] text-[var(--color-muted-foreground)] underline-offset-4 hover:text-[var(--color-foreground)] hover:underline"
          >
            ← Back to sign in
          </Link>
        </div>
      </div>
    </div>
  );
}

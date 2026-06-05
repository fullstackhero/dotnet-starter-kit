import { useState, type FormEvent } from "react";
import { Link, Navigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import {
  AlertCircle,
  ArrowRight,
  Building2,
  Check,
  Loader2,
  Mail,
  MailCheck,
  ShieldCheck,
} from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { requestPasswordReset } from "@/api/users";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { env } from "@/env";

/**
 * Forgot-password — admin variant.
 *
 * Same security contract as the dashboard variant: server returns 200
 * for any input, so the UI must NOT branch on response shape to imply
 * account existence. Always render the same "check your inbox" success.
 */
export function ForgotPasswordPage() {
  const { isAuthenticated } = useAuth();
  const [email, setEmail] = useState("");
  const [tenant, setTenant] = useState(env.defaultTenant);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: () => requestPasswordReset({ email, tenant }),
    onSuccess: () => setSubmitted(true),
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      setError(detail);
    },
  });

  if (isAuthenticated) return <Navigate to="/" replace />;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    mutation.mutate();
  };

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

        {/* Form card */}
        <div className="rounded-xl border border-[var(--color-border)] bg-[oklch(from_var(--color-card)_l_c_h_/_0.85)] shadow-[0_1px_3px_oklch(0_0_0_/_0.04),0_8px_24px_oklch(0_0_0_/_0.06)] backdrop-blur-xl">
          <div className="px-6 py-7 sm:px-8 sm:py-9">
            {submitted ? (
              <div className="fsh-enter space-y-5 text-center">
                <div className="grid place-items-center">
                  <span
                    aria-hidden
                    className="grid size-14 place-items-center rounded-2xl bg-[oklch(from_var(--color-success)_l_c_h_/_0.10)] text-[var(--color-success)]"
                  >
                    <MailCheck className="size-6" />
                  </span>
                </div>
                <div>
                  <h1 className="mb-1.5 font-display text-[22px] font-semibold tracking-tight text-[var(--color-foreground)]">
                    Check your{" "}
                    <span className="text-[var(--color-primary)]">inbox</span>
                  </h1>
                  <p className="text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
                    If an account exists for{" "}
                    <span className="text-[var(--color-foreground)]">{email}</span> in tenant{" "}
                    <span className="text-[var(--color-foreground)]">{tenant}</span>, a one-time
                    reset link is on its way. The link expires in 30 minutes.
                  </p>
                </div>
                <ul className="space-y-1.5 text-left text-[12.5px] text-[var(--color-muted-foreground)]">
                  <li className="flex items-start gap-2">
                    <Check className="mt-0.5 size-3.5 shrink-0 text-[var(--color-success)]" />
                    Didn't get it? Wait a minute, then check spam.
                  </li>
                  <li className="flex items-start gap-2">
                    <Check className="mt-0.5 size-3.5 shrink-0 text-[var(--color-success)]" />
                    Still nothing? Confirm the email + tenant and retry.
                  </li>
                </ul>
                <div className="flex items-center gap-2 pt-1">
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={() => {
                      setSubmitted(false);
                      setError(null);
                    }}
                  >
                    Try a different address
                  </Button>
                  <Link to="/login" className="ml-auto">
                    <Button type="button" variant="outline">
                      Back to sign in
                    </Button>
                  </Link>
                </div>
              </div>
            ) : (
              <>
                <div className="mb-6 sm:mb-8">
                  <h1 className="mb-1.5 font-display text-[22px] font-semibold tracking-tight text-[var(--color-foreground)]">
                    Reset your{" "}
                    <span className="text-[var(--color-primary)]">password</span>
                  </h1>
                  <p className="text-[13px] text-[var(--color-muted-foreground)]">
                    Enter the email + tenant you sign in with. We'll dispatch a one-time link.
                  </p>
                </div>

                <form
                  onSubmit={onSubmit}
                  className="space-y-5"
                  noValidate
                  aria-describedby={error ? "forgot-error" : undefined}
                >
                  <div className="space-y-1.5">
                    <Label
                      htmlFor="reset-tenant"
                      className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
                    >
                      Tenant
                    </Label>
                    <div className="relative">
                      <Building2 className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]" />
                      <Input
                        id="reset-tenant"
                        value={tenant}
                        onChange={(e) => setTenant(e.target.value)}
                        required
                        autoComplete="organization"
                        placeholder="root"
                        aria-invalid={error ? true : undefined}
                        aria-describedby={error ? "forgot-error" : undefined}
                        className="h-11 pl-10 text-[14px]"
                      />
                    </div>
                  </div>
                  <div className="space-y-1.5">
                    <Label
                      htmlFor="reset-email"
                      className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
                    >
                      Email
                    </Label>
                    <div className="relative">
                      <Mail className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]" />
                      <Input
                        id="reset-email"
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                        autoComplete="email"
                        autoFocus
                        placeholder="operator@root.example"
                        aria-invalid={error ? true : undefined}
                        aria-describedby={error ? "forgot-error" : undefined}
                        className="h-11 pl-10 text-[14px]"
                      />
                    </div>
                  </div>

                  {error && (
                    <div
                      id="forgot-error"
                      role="alert"
                      className={cn(
                        "fsh-enter flex items-start gap-2 rounded-lg border px-3 py-2 text-sm",
                        "border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)]",
                        "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)]",
                        "text-[var(--color-destructive)]",
                      )}
                    >
                      <AlertCircle className="mt-0.5 size-4 shrink-0" />
                      <span className="leading-snug">{error}</span>
                    </div>
                  )}

                  <div className="pt-1.5">
                    <Button
                      type="submit"
                      disabled={mutation.isPending || !email || !tenant}
                      className="group h-11 w-full text-[14px] font-semibold"
                    >
                      {mutation.isPending ? (
                        <>
                          <Loader2 className="size-4 animate-spin" />
                          <span>Dispatching link…</span>
                        </>
                      ) : (
                        <>
                          <span>Send reset link</span>
                          <ArrowRight className="size-[14px] opacity-60 transition-all duration-200 group-hover:translate-x-0.5 group-hover:opacity-100" />
                        </>
                      )}
                    </Button>
                  </div>
                </form>
              </>
            )}
          </div>
        </div>

        <div className="mt-6 text-center text-[12.5px] text-[var(--color-muted-foreground)]">
          Remembered it?{" "}
          <Link
            to="/login"
            className="text-[var(--color-foreground)] underline-offset-4 hover:underline"
          >
            Sign in
          </Link>
        </div>

        <div className="mt-4 flex items-center justify-center gap-1.5 text-[11px] text-[var(--color-muted-foreground)]">
          <ShieldCheck className="size-3" />
          <span>Encrypted in transit · JWT-secured session</span>
        </div>
      </div>
    </div>
  );
}

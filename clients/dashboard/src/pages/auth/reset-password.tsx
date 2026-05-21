import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, Navigate, useNavigate, useSearchParams } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import {
  AlertCircle,
  ArrowRight,
  Check,
  Eye,
  EyeOff,
  Loader2,
  ShieldCheck,
} from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { AuthHeadline, AuthShell } from "@/components/auth/auth-shell";
import { resetPassword } from "@/api/identity";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

/**
 * Reset-password — step 2 of the reset flow. The user lands here from a
 * link emailed by the forgot-password endpoint:
 *
 *   /reset-password?token=<urlencoded>&email=<urlencoded>&tenant=<id>
 *
 * Token + email + tenant come from URL; password comes from the form.
 * On success we toast and redirect to /login (existing JWTs stay valid
 * until natural expiry — a fresh sign-in under the new password is the
 * cleanest UX).
 */

type Strength = "weak" | "fair" | "strong";

function scorePassword(value: string): Strength | null {
  if (value.length === 0) return null;
  if (value.length < 8) return "weak";

  let score = 0;
  if (/[a-z]/.test(value)) score++;
  if (/[A-Z]/.test(value)) score++;
  if (/\d/.test(value)) score++;
  if (/[^A-Za-z0-9]/.test(value)) score++;
  if (value.length >= 12) score++;

  if (score <= 2) return "weak";
  if (score === 3) return "fair";
  return "strong";
}

const STRENGTH_META: Record<Strength, { label: string; fill: string; bar: string }> = {
  weak: {
    label: "Weak",
    fill: "bg-[var(--color-destructive)]",
    bar: "w-1/3",
  },
  fair: {
    label: "Fair",
    fill: "bg-[var(--color-warning)]",
    bar: "w-2/3",
  },
  strong: {
    label: "Strong",
    fill: "bg-[var(--color-success)]",
    bar: "w-full",
  },
};

export function ResetPasswordPage() {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();

  // All three are required for the server call. We treat the page as
  // "malformed link" if any are missing rather than letting the user
  // submit a doomed request.
  const token = params.get("token") ?? "";
  const email = params.get("email") ?? "";
  const tenant = params.get("tenant") ?? "";

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);

  const strength = useMemo(() => scorePassword(password), [password]);
  const matches = password.length > 0 && password === confirm;

  const mutation = useMutation({
    mutationFn: () => resetPassword({ email, password, token, tenant }),
    onSuccess: () => {
      toast.success("Password updated", {
        description: "Sign in with your new password to continue.",
      });
      navigate("/login", { replace: true });
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      setError(detail);
    },
  });

  // Keep the error in sync with the form: typing after an error should
  // clear it rather than persist a stale message under the new input.
  useEffect(() => {
    setError(null);
  }, [password, confirm]);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const malformed = !token || !email || !tenant;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!matches) {
      setError("Passwords don't match.");
      return;
    }
    if (password.length < 8) {
      setError("Use at least 8 characters.");
      return;
    }
    mutation.mutate();
  };

  return (
    <AuthShell
      footer={
        <span>
          Changed your mind?{" "}
          <Link
            to="/login"
            className="text-[var(--color-foreground)] underline-offset-4 hover:underline"
          >
            Sign in
          </Link>
        </span>
      }
    >
      {malformed ? (
        <div className="space-y-4">
          <div className="mb-2">
            <AuthHeadline lead="This link is" accent="incomplete" />
            <p className="text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
              The reset link is missing one of{" "}
              <span className="text-[var(--color-foreground)]">token</span>,{" "}
              <span className="text-[var(--color-foreground)]">email</span>, or{" "}
              <span className="text-[var(--color-foreground)]">tenant</span>.
              Some email clients clip long URLs — try copy-pasting the full
              link from the original email into your browser's address bar.
            </p>
          </div>
          <div className="flex gap-2 pt-1">
            <Link to="/forgot-password">
              <Button type="button" variant="outline">
                Request a new link
              </Button>
            </Link>
            <Link to="/login">
              <Button type="button" variant="ghost">
                Back to sign in
              </Button>
            </Link>
          </div>
        </div>
      ) : (
        <>
          <div className="mb-6 sm:mb-8">
            <AuthHeadline lead="Set a new" accent="password" />
            <p className="text-[13px] text-[var(--color-muted-foreground)]">
              Resetting password for{" "}
              <span className="text-[var(--color-foreground)]">{email}</span> on{" "}
              <span className="text-[var(--color-foreground)]">{tenant}</span>.
            </p>
          </div>

          <form onSubmit={onSubmit} className="space-y-5" noValidate aria-describedby={error ? "reset-error" : undefined}>
            <div className="space-y-1.5">
              <Label
                htmlFor="new-password"
                className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
              >
                New password
              </Label>
              <div className="relative">
                <Input
                  id="new-password"
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="At least 8 characters"
                  required
                  autoComplete="new-password"
                  autoFocus
                  minLength={8}
                  aria-invalid={error ? true : undefined}
                  aria-describedby={error ? "reset-error" : undefined}
                  className="h-11 pr-11 text-[14px]"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  aria-label={showPassword ? "Hide password" : "Show password"}
                  className="absolute right-3.5 top-1/2 grid h-6 w-6 -translate-y-1/2 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]"
                >
                  {showPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                </button>
              </div>

              {strength && (
                <div className="fsh-enter flex items-center gap-2 pt-1.5">
                  <div className="h-1 flex-1 overflow-hidden rounded-full bg-[var(--color-muted)]">
                    <div
                      className={cn(
                        "h-full transition-all duration-200",
                        STRENGTH_META[strength].fill,
                        STRENGTH_META[strength].bar,
                      )}
                    />
                  </div>
                  <span className="min-w-[3.5rem] text-right text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                    {STRENGTH_META[strength].label}
                  </span>
                </div>
              )}
            </div>

            <div className="space-y-1.5">
              <Label
                htmlFor="confirm-password"
                className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
              >
                Confirm password
              </Label>
              <div className="relative">
                <Input
                  id="confirm-password"
                  type={showConfirm ? "text" : "password"}
                  value={confirm}
                  onChange={(e) => setConfirm(e.target.value)}
                  placeholder="Re-enter password"
                  required
                  autoComplete="new-password"
                  minLength={8}
                  aria-invalid={error ? true : undefined}
                  aria-describedby={error ? "reset-error" : undefined}
                  className="h-11 pr-11 text-[14px]"
                />
                <button
                  type="button"
                  onClick={() => setShowConfirm((v) => !v)}
                  aria-label={showConfirm ? "Hide password" : "Show password"}
                  className="absolute right-3.5 top-1/2 grid h-6 w-6 -translate-y-1/2 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]"
                >
                  {showConfirm ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                </button>
              </div>

              {confirm.length > 0 && (
                <div
                  className={cn(
                    "flex items-center gap-1.5 pt-1 text-[11.5px]",
                    matches
                      ? "text-[var(--color-success)]"
                      : "text-[var(--color-muted-foreground)]",
                  )}
                >
                  <Check
                    className={cn(
                      "size-3.5",
                      matches ? "opacity-100" : "opacity-40",
                    )}
                  />
                  <span>{matches ? "Passwords match" : "Doesn't match yet"}</span>
                </div>
              )}
            </div>

            {error && (
              <div
                id="reset-error"
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
                disabled={mutation.isPending || !matches || password.length < 8}
                className="group h-11 w-full text-[14px] font-semibold"
              >
                {mutation.isPending ? (
                  <>
                    <Loader2 className="size-4 animate-spin" />
                    <span>Updating password…</span>
                  </>
                ) : (
                  <>
                    <ShieldCheck className="size-4" />
                    <span>Set new password</span>
                    <ArrowRight className="size-[14px] opacity-60 transition-all duration-200 group-hover:translate-x-0.5 group-hover:opacity-100" />
                  </>
                )}
              </Button>
            </div>
          </form>
        </>
      )}
    </AuthShell>
  );
}

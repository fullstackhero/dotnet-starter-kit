import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { ClipboardCheck, Copy, KeyRound, ShieldCheck, ShieldOff } from "lucide-react";
import { toast } from "sonner";
import { changePassword, getMyProfile } from "@/api/users";
import {
  disableTwoFactor,
  enrollTwoFactor,
  verifyEnrollTwoFactor,
  type TwoFactorEnrollmentResponse,
} from "@/api/two-factor";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  ErrorBand,
  Field,
  FormActions,
  FormSection,
  FormShell,
  LoadingRow,
} from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

/**
 * SecuritySettings — combines password change + 2FA enrollment/disable
 * into one tab. Profile query fuels both: we read TwoFactorEnabled to
 * decide whether to render the enroll wizard or the disable controls.
 */
export function SecuritySettings() {
  const profile = useQuery({ queryKey: ["identity", "profile"], queryFn: getMyProfile });

  if (profile.isLoading) return <LoadingRow label="Loading security state" />;
  if (profile.isError) {
    return (
      <ErrorBand
        message={
          profile.error instanceof ApiRequestError
            ? profile.error.problem?.detail ?? profile.error.message
            : "Failed to load security state."
        }
      />
    );
  }

  const twoFactorEnabled = profile.data?.twoFactorEnabled ?? false;

  return (
    <div className="space-y-7">
      <PasswordSection />
      <TwoFactorSection enabled={twoFactorEnabled} />
    </div>
  );
}

// ─── Password section ───────────────────────────────────────────────────

const passwordSchema = z
  .object({
    current: z.string().min(1, "Required."),
    next: z.string().min(8, "At least 8 characters."),
    confirm: z.string().min(8),
  })
  .refine((v) => v.next === v.confirm, {
    path: ["confirm"],
    message: "Passwords don't match.",
  })
  .refine((v) => v.next !== v.current, {
    path: ["next"],
    message: "New password must differ from the current one.",
  });

type PasswordValues = z.infer<typeof passwordSchema>;

function PasswordSection() {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<PasswordValues>({
    resolver: zodResolver(passwordSchema),
    defaultValues: { current: "", next: "", confirm: "" },
  });

  const mutation = useMutation({
    mutationFn: (v: PasswordValues) =>
      changePassword({
        password: v.current,
        newPassword: v.next,
        confirmNewPassword: v.confirm,
      }),
    onSuccess: () => {
      toast.success("Password changed", {
        description: "Other active sessions remain valid until you revoke them.",
      });
      reset();
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Change failed", { description: detail });
    },
  });

  const onSubmit = handleSubmit((v) => mutation.mutate(v));
  const submitting = isSubmitting || mutation.isPending;

  return (
    <form onSubmit={onSubmit}>
      <FormShell>
        <FormSection
          title="Password"
          description="Changing your password does NOT revoke other sessions automatically — visit the Sessions tab to sign out other devices."
        >
          <Field id="pw-current" label="Current password" required error={errors.current?.message}>
            <Input
              id="pw-current"
              type="password"
              autoComplete="current-password"
              className="font-mono"
              aria-invalid={errors.current ? true : undefined}
              {...register("current")}
            />
          </Field>
          <Field id="pw-next" label="New password" required hint="At least 8 characters." error={errors.next?.message}>
            <Input
              id="pw-next"
              type="password"
              autoComplete="new-password"
              className="font-mono"
              aria-invalid={errors.next ? true : undefined}
              {...register("next")}
            />
          </Field>
          <Field id="pw-confirm" label="Confirm new password" required error={errors.confirm?.message}>
            <Input
              id="pw-confirm"
              type="password"
              autoComplete="new-password"
              className="font-mono"
              aria-invalid={errors.confirm ? true : undefined}
              {...register("confirm")}
            />
          </Field>
        </FormSection>
        <FormActions>
          <Button type="submit" disabled={submitting}>
            <KeyRound className="mr-1 h-3.5 w-3.5" />
            {submitting ? "Updating…" : "Update password"}
          </Button>
        </FormActions>
      </FormShell>
    </form>
  );
}

// ─── Two-factor section ─────────────────────────────────────────────────

function TwoFactorSection({ enabled }: { enabled: boolean }) {
  if (enabled) {
    return <TwoFactorDisable />;
  }
  return <TwoFactorEnroll />;
}

function TwoFactorEnroll() {
  const queryClient = useQueryClient();
  const [enrollment, setEnrollment] = useState<TwoFactorEnrollmentResponse | null>(null);
  const [code, setCode] = useState("");
  const [qrSvg, setQrSvg] = useState<string | null>(null);
  const [copiedKey, setCopiedKey] = useState(false);

  const beginMutation = useMutation({
    mutationFn: enrollTwoFactor,
    onSuccess: (data) => setEnrollment(data),
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Enrollment failed", { description: detail });
    },
  });

  const verifyMutation = useMutation({
    mutationFn: (otp: string) => verifyEnrollTwoFactor(otp),
    onSuccess: (data) => {
      if (data.success) {
        toast.success("Two-factor enabled", {
          description: "Future logins require a 6-digit code from your authenticator.",
        });
        setEnrollment(null);
        setCode("");
        setQrSvg(null);
        queryClient.invalidateQueries({ queryKey: ["identity", "profile"] });
      } else {
        toast.error("Verification failed", { description: "That code didn't match. Try again." });
      }
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Verification failed", { description: detail });
    },
  });

  // Render the QR as inline SVG when the otpauth URI changes — keeps the
  // image source-of-truth in JS without an extra <canvas>.
  useEffect(() => {
    if (!enrollment) {
      setQrSvg(null);
      return;
    }
    let cancelled = false;
    // Lazy-load the ~50KB qrcode lib only when 2FA enrollment actually starts.
    import("qrcode")
      .then(({ default: QRCode }) =>
        QRCode.toString(enrollment.authenticatorUri, {
          type: "svg",
          margin: 1,
          width: 200,
          errorCorrectionLevel: "M",
        }),
      )
      .then((svg) => {
        if (cancelled) return;
        // Strip the default white background so the QR adapts to dark mode.
        // currentColor + black-on-transparent reads well on both surfaces.
        const themed = svg
          .replace(/fill="#ffffff"/gi, 'fill="transparent"')
          .replace(/fill="#FFFFFF"/gi, 'fill="transparent"')
          .replace(/fill="#000000"/gi, 'fill="currentColor"')
          .replace(/fill="#000"/gi, 'fill="currentColor"');
        setQrSvg(themed);
      })
      .catch(() => setQrSvg(null));
    return () => {
      cancelled = true;
    };
  }, [enrollment]);

  const copyKey = async () => {
    if (!enrollment) return;
    try {
      await navigator.clipboard.writeText(enrollment.sharedKey);
      setCopiedKey(true);
      window.setTimeout(() => setCopiedKey(false), 1500);
    } catch {
      /* noop */
    }
  };

  return (
    <FormShell>
      <FormSection
        title="Two-factor authentication"
        description={
          <>
            Adds an authenticator app code on top of your password. Recommended for every
            operator account.
            <Badge variant="outline" className="ml-2 font-mono uppercase tracking-[0.14em]">
              off
            </Badge>
          </>
        }
      >
        {!enrollment ? (
          <div className="flex flex-wrap items-center gap-3">
            <Button onClick={() => beginMutation.mutate()} disabled={beginMutation.isPending}>
              <ShieldCheck className="mr-1.5 h-3.5 w-3.5" />
              {beginMutation.isPending ? "Generating…" : "Enable two-factor"}
            </Button>
            <span className="text-xs text-[var(--color-muted-foreground)]">
              You'll scan a QR code in your authenticator app (1Password, Google Authenticator, Authy…).
            </span>
          </div>
        ) : (
          <div className="space-y-5">
            <div className="grid gap-5 sm:grid-cols-[14rem_1fr] sm:items-start">
              <div className="grid h-52 w-52 place-items-center rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-2 text-[var(--color-foreground)]">
                {qrSvg ? (
                  <div
                    aria-label="Two-factor QR code"
                    role="img"
                    className="h-full w-full [&_svg]:h-full [&_svg]:w-full"
                    // qrSvg is locally generated by the qrcode lib from the
                    // authenticator URI (not server/user HTML) — safe to inline.
                    dangerouslySetInnerHTML={{ __html: qrSvg }}
                  />
                ) : (
                  <span className="meta text-[var(--color-muted-foreground)]">
                    Rendering<span className="caret text-[var(--color-accent-signal)]" />
                  </span>
                )}
              </div>
              <div className="space-y-3">
                <div>
                  <div className="meta text-[var(--color-muted-foreground)]">
                    can't scan? enter manually
                  </div>
                  <div className="mt-1 flex items-center gap-2">
                    <code className="code-chip break-all">{enrollment.sharedKey}</code>
                    <button
                      type="button"
                      onClick={copyKey}
                      className="inline-flex h-7 items-center gap-1 rounded-md px-2 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
                    >
                      {copiedKey ? (
                        <>
                          <ClipboardCheck className="h-3 w-3" /> copied
                        </>
                      ) : (
                        <>
                          <Copy className="h-3 w-3" /> copy
                        </>
                      )}
                    </button>
                  </div>
                </div>

                <Field id="totp-code" label="6-digit code from your app" required>
                  <Input
                    id="totp-code"
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    placeholder="123 456"
                    maxLength={8}
                    value={code}
                    onChange={(e) => setCode(e.target.value.replace(/\s/g, ""))}
                    className={cn("font-mono text-lg tracking-[0.4em]", code.length >= 6 && "border-[var(--color-accent-signal)]")}
                  />
                </Field>

                <div className="flex flex-wrap items-center gap-2 pt-1">
                  <Button
                    onClick={() => verifyMutation.mutate(code)}
                    disabled={code.length < 6 || verifyMutation.isPending}
                    variant="signal"
                  >
                    {verifyMutation.isPending ? "Verifying…" : "Confirm & enable"}
                  </Button>
                  <Button
                    variant="ghost"
                    onClick={() => {
                      setEnrollment(null);
                      setCode("");
                    }}
                    disabled={verifyMutation.isPending}
                  >
                    Cancel
                  </Button>
                </div>
              </div>
            </div>
          </div>
        )}
      </FormSection>
    </FormShell>
  );
}

function TwoFactorDisable() {
  const queryClient = useQueryClient();
  const [password, setPassword] = useState("");

  const mutation = useMutation({
    mutationFn: (pw: string) => disableTwoFactor(pw),
    onSuccess: (data) => {
      if (data.success) {
        toast.success("Two-factor disabled");
        setPassword("");
        queryClient.invalidateQueries({ queryKey: ["identity", "profile"] });
      } else {
        toast.error("Disable failed", { description: "Password verification failed." });
      }
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Disable failed", { description: detail });
    },
  });

  return (
    <FormShell>
      <FormSection
        title="Two-factor authentication"
        description={
          <>
            Two-factor is currently enabled on your account. Confirm your password to disable —
            this rotates the authenticator secret, so a fresh enroll will generate a new QR.
            <Badge variant="success" className="ml-2 font-mono uppercase tracking-[0.14em]">
              enabled
            </Badge>
          </>
        }
      >
        <Field id="disable-pw" label="Current password" required>
          <Input
            id="disable-pw"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="font-mono"
          />
        </Field>
      </FormSection>
      <FormActions>
        <Button
          type="button"
          variant="destructive"
          onClick={() => mutation.mutate(password)}
          disabled={password.length === 0 || mutation.isPending}
        >
          <ShieldOff className="mr-1 h-3.5 w-3.5" />
          {mutation.isPending ? "Disabling…" : "Disable two-factor"}
        </Button>
      </FormActions>
    </FormShell>
  );
}

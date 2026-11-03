import { useEffect, useRef, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { AlertCircle, ArrowLeft, ArrowRight, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/cn";

const TWO_FACTOR_CODE_LENGTH = 6;

type TwoFactorStepProps = {
  email: string;
  code: string;
  onCodeChange: (code: string) => void;
  onSubmit: () => void;
  onBack: () => void;
  submitting: boolean;
  error: string | null;
};

/**
 * Second step of the sign-in card, shown after the token call answers
 * `Identity.TwoFactorRequired`. Collects the 6-digit authenticator code; the
 * login page owns the credentials and re-issues the token call with it.
 */
export function TwoFactorStep({
  email,
  code,
  onCodeChange,
  onSubmit,
  onBack,
  submitting,
  error,
}: TwoFactorStepProps) {
  const { t } = useTranslation("auth");
  const inputRef = useRef<HTMLInputElement>(null);

  // Put the cursor back in the field after a rejected code (the page clears it).
  useEffect(() => {
    if (error && code === "") inputRef.current?.focus();
  }, [error, code]);

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (code.length === TWO_FACTOR_CODE_LENGTH && !submitting) onSubmit();
  };

  return (
    <>
      <div className="mb-6 sm:mb-8">
        <h1 className="mb-1.5 font-display text-[22px] font-semibold tracking-tight text-[var(--color-foreground)]">
          {t("twoFactor.title")}
        </h1>
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          {t("twoFactor.subtitle")}
          {email ? (
            <>
              {" "}
              {t("twoFactor.subtitleAs")}{" "}
              <span className="font-medium text-[var(--color-foreground)]">{email}</span>
            </>
          ) : null}
          .
        </p>
      </div>

      <form
        onSubmit={handleSubmit}
        className="space-y-5"
        noValidate
        aria-describedby={error ? "two-factor-error" : undefined}
      >
        <div className="space-y-1.5">
          <Label
            htmlFor="two-factor-code"
            className="block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
          >
            {t("twoFactor.code")}
          </Label>
          <Input
            ref={inputRef}
            id="two-factor-code"
            name="twoFactorCode"
            value={code}
            onChange={(e) => onCodeChange(e.target.value.replace(/\D/g, "").slice(0, TWO_FACTOR_CODE_LENGTH))}
            inputMode="numeric"
            autoComplete="one-time-code"
            pattern="[0-9]*"
            maxLength={TWO_FACTOR_CODE_LENGTH}
            placeholder={t("twoFactor.codePlaceholder")}
            autoFocus
            required
            aria-invalid={error ? true : undefined}
            className="h-11 font-mono text-[16px] tracking-[0.4em]"
          />
        </div>

        {error && (
          <div
            id="two-factor-error"
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

        <div className="space-y-3 pt-1.5">
          <Button
            type="submit"
            disabled={submitting || code.length !== TWO_FACTOR_CODE_LENGTH}
            className="group h-11 w-full text-[14px] font-semibold"
          >
            {submitting ? (
              <>
                <Loader2 className="size-4 animate-spin" />
                <span>{t("twoFactor.submitting")}</span>
              </>
            ) : (
              <>
                <span>{t("twoFactor.submit")}</span>
                <ArrowRight className="size-[14px] opacity-60 transition-all duration-200 group-hover:translate-x-0.5 group-hover:opacity-100" />
              </>
            )}
          </Button>
          <button
            type="button"
            onClick={onBack}
            disabled={submitting}
            className="mx-auto flex cursor-pointer items-center gap-1.5 text-[12px] font-medium text-[var(--color-muted-foreground)] underline-offset-4 transition-colors hover:text-[var(--color-primary)] hover:underline disabled:pointer-events-none disabled:opacity-50"
          >
            <ArrowLeft className="size-3" />
            <span>{t("twoFactor.back")}</span>
          </button>
        </div>
      </form>
    </>
  );
}

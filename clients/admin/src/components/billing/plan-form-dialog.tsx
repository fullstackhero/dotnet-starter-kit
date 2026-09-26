import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { z } from "zod";
import { CreditCard, Gauge } from "lucide-react";
import { toast } from "sonner";
import {
  createPlan,
  updatePlan,
  type BillingPlanDto,
  type PlanInterval,
  type QuotaResource,
} from "@/api/billing";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field, Select, type SelectOption } from "@/components/list";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { ApiRequestError } from "@/lib/api-client";

const PLAN_KEY_PATTERN = /^[a-z0-9][a-z0-9-]{0,62}[a-z0-9]$/;

// A money/rate field is a free-text decimal string. These refinements run
// client-side so a negative price is rejected before any network call (the
// server also rejects it, but we don't rely on that). Messages are i18n keys,
// resolved to text at display time via t().
const NON_NEGATIVE_MSG = "planForm.validation.nonNegative";
const REQUIRED_MSG = "planForm.validation.required";

/** Required non-negative decimal (e.g. monthly base price). */
const requiredNonNegative = z
  .string()
  .trim()
  .min(1, REQUIRED_MSG)
  .refine((v) => Number.isFinite(Number(v)) && Number(v) >= 0, NON_NEGATIVE_MSG);

/** Optional non-negative decimal (blank allowed → omitted). */
const optionalNonNegative = z
  .string()
  .trim()
  .refine((v) => v === "" || (Number.isFinite(Number(v)) && Number(v) >= 0), NON_NEGATIVE_MSG);

const OVERAGE_RESOURCES: { key: QuotaResource; labelKey: string; placeholder: string }[] = [
  { key: "ApiCalls", labelKey: "planForm.overage.apiCalls", placeholder: "0.0010" },
  { key: "StorageBytes", labelKey: "planForm.overage.storageBytes", placeholder: "0.00000001" },
  { key: "Users", labelKey: "planForm.overage.users", placeholder: "5.00" },
  { key: "ActiveFeatureFlags", labelKey: "planForm.overage.featureFlags", placeholder: "1.00" },
];

type OverageState = Record<string, string>;

function toOverageNumbers(state: OverageState): Record<string, number> | null {
  const out: Record<string, number> = {};
  let any = false;
  for (const { key } of OVERAGE_RESOURCES) {
    const raw = state[key];
    if (raw === undefined || raw.trim() === "") continue;
    const n = Number(raw);
    // Submission is blocked upstream when a value is invalid, so anything that
    // reaches here is a non-negative finite number.
    if (!Number.isFinite(n) || n < 0) continue;
    out[key] = n;
    any = true;
  }
  return any ? out : null;
}

/** First validation message for a value against a schema, or undefined when valid. */
function fieldError(schema: z.ZodTypeAny, value: string): string | undefined {
  const result = schema.safeParse(value);
  return result.success ? undefined : result.error.issues[0]?.message;
}

function describe(err: unknown, fallback: string): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return fallback;
}

function SectionLabel({
  icon: Icon,
  title,
  description,
}: {
  icon: React.ComponentType<{ className?: string }>;
  title: string;
  description: string;
}) {
  return (
    <div className="flex items-start gap-2.5 pb-1">
      <span
        aria-hidden
        className="mt-0.5 grid h-6 w-6 shrink-0 place-items-center rounded-md bg-[var(--color-accent)] text-[var(--color-muted-foreground)]"
      >
        <Icon className="h-3.5 w-3.5" />
      </span>
      <div className="min-w-0">
        <p className="text-[12.5px] font-semibold text-[var(--color-foreground)]">{title}</p>
        <p className="text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">{description}</p>
      </div>
    </div>
  );
}

/**
 * Create or edit a billing plan in a dialog. Pass `plan` to edit (key + currency are immutable then),
 * omit it to create. On success it invalidates the plans cache and closes.
 */
export function PlanFormDialog({
  open,
  onOpenChange,
  plan,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  plan?: BillingPlanDto;
}) {
  const { t } = useTranslation("billing");
  const queryClient = useQueryClient();
  const isEdit = !!plan;

  const INTERVAL_OPTIONS: SelectOption<PlanInterval>[] = [
    { value: "Monthly", label: t("planForm.interval.monthly"), hint: t("planForm.interval.monthlyHint") },
    { value: "Yearly", label: t("planForm.interval.yearly"), hint: t("planForm.interval.yearlyHint") },
  ];

  const [key, setKey] = useState("");
  const [name, setName] = useState("");
  const [currency, setCurrency] = useState("USD");
  const [monthlyBasePrice, setMonthlyBasePrice] = useState("");
  const [interval, setInterval] = useState<PlanInterval>("Monthly");
  const [annualPrice, setAnnualPrice] = useState("");
  const [overage, setOverage] = useState<OverageState>({});

  // Reset/populate whenever the dialog opens (or the target plan changes).
  useEffect(() => {
    if (!open) return;
    setKey(plan?.key ?? "");
    setName(plan?.name ?? "");
    setCurrency(plan?.currency ?? "USD");
    setMonthlyBasePrice(plan ? String(plan.monthlyBasePrice) : "");
    setInterval(plan?.interval === "Yearly" ? "Yearly" : "Monthly");
    setAnnualPrice(plan?.annualPrice != null ? String(plan.annualPrice) : "");
    const next: OverageState = {};
    for (const [resource, rate] of Object.entries(plan?.overageRates ?? {})) {
      if (rate !== undefined && rate !== null) next[resource] = String(rate);
    }
    setOverage(next);
  }, [open, plan]);

  const keyInvalid = !isEdit && key.length > 0 && !PLAN_KEY_PATTERN.test(key);
  const priceNum = Number(monthlyBasePrice);
  // Only surface the price error once something's been typed; submit-time
  // validation (onSubmit) still blocks an empty required field.
  const priceError =
    monthlyBasePrice.length > 0 ? fieldError(requiredNonNegative, monthlyBasePrice) : undefined;
  const annualNum = Number(annualPrice);
  const annualError = fieldError(optionalNonNegative, annualPrice);
  const annualPricePayload = interval === "Yearly" && annualPrice.trim().length > 0 ? annualNum : null;

  // Per-resource overage validation — a negative or non-numeric rate blocks submit.
  const overageErrors = useMemo(() => {
    const out: Partial<Record<string, string>> = {};
    for (const { key: resKey } of OVERAGE_RESOURCES) {
      const err = fieldError(optionalNonNegative, overage[resKey] ?? "");
      if (err) out[resKey] = err;
    }
    return out;
  }, [overage]);
  const hasOverageError = Object.keys(overageErrors).length > 0;
  // Aggregate validity for disabling submit. Monthly price is required + non-negative.
  const pricingInvalid =
    !!fieldError(requiredNonNegative, monthlyBasePrice) || !!annualError || hasOverageError;

  const onClose = () => onOpenChange(false);

  const createMutation = useMutation({
    mutationFn: createPlan,
    onSuccess: () => {
      toast.success(t("planForm.toast.created", { name }));
      queryClient.invalidateQueries({ queryKey: ["billing", "plans"] });
      onClose();
    },
    onError: (err) => toast.error(t("planForm.toast.createFailed"), { description: describe(err, t("planForm.toast.createFailedDesc")) }),
  });

  const updateMutation = useMutation({
    mutationFn: updatePlan,
    onSuccess: () => {
      toast.success(t("planForm.toast.updated", { name }));
      queryClient.invalidateQueries({ queryKey: ["billing", "plans"] });
      onClose();
    },
    onError: (err) => toast.error(t("planForm.toast.updateFailed"), { description: describe(err, t("planForm.toast.updateFailedDesc")) }),
  });

  const pending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (pricingInvalid) return;
    const overageRates = toOverageNumbers(overage);

    if (isEdit && plan) {
      updateMutation.mutate({
        planId: plan.id,
        name: name.trim(),
        monthlyBasePrice: priceNum,
        overageRates,
        interval,
        annualPrice: annualPricePayload,
      });
      return;
    }
    if (keyInvalid) return;
    createMutation.mutate({
      key: key.trim(),
      name: name.trim(),
      currency: currency.trim().toUpperCase(),
      monthlyBasePrice: priceNum,
      overageRates,
      interval,
      annualPrice: annualPricePayload,
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent size="lg">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="grid h-9 w-9 shrink-0 place-items-center rounded-xl
                bg-[oklch(from_var(--color-primary)_l_c_h_/_0.12)] text-[var(--color-primary)]
                ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]"
            >
              <CreditCard className="h-[18px] w-[18px]" />
            </span>
            <DialogTitle className="text-[16px]">{isEdit ? t("planForm.editTitle") : t("planForm.newTitle")}</DialogTitle>
          </div>
          <DialogDescription className="mt-1">
            {isEdit ? t("planForm.editDescription") : t("planForm.newDescription")}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit}>
          <DialogBody className="space-y-6">
            {/* ── Details ── */}
            <div className="space-y-3">
              <SectionLabel
                icon={CreditCard}
                title={t("planForm.section.details.title")}
                description={t("planForm.section.details.description")}
              />
              <div className="h-px bg-[var(--color-border)] opacity-60" />
              <div className="grid gap-4 sm:grid-cols-2">
                <Field
                  id="pf-key"
                  label={t("planForm.field.key")}
                  hint={t("planForm.field.keyHint")}
                  required={!isEdit}
                  error={keyInvalid ? t("planForm.field.keyError") : undefined}
                >
                  <Input
                    id="pf-key"
                    value={key}
                    onChange={(e) => setKey(e.target.value)}
                    placeholder="pro"
                    className="font-mono"
                    disabled={isEdit}
                    autoComplete="off"
                  />
                </Field>
                <Field id="pf-name" label={t("planForm.field.name")} required>
                  <Input id="pf-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Pro" />
                </Field>
                <Field id="pf-currency" label={t("planForm.field.currency")} hint={t("planForm.field.currencyHint")} required={!isEdit}>
                  <Input
                    id="pf-currency"
                    value={currency}
                    onChange={(e) => setCurrency(e.target.value.toUpperCase())}
                    placeholder="USD"
                    className="font-mono"
                    disabled={isEdit}
                    autoComplete="off"
                  />
                </Field>
                <Field
                  id="pf-monthlyBasePrice"
                  label={t("planForm.field.monthlyBasePrice")}
                  hint={t("planForm.field.monthlyBasePriceHint")}
                  required
                  error={priceError ? t(priceError) : undefined}
                >
                  <Input
                    id="pf-monthlyBasePrice"
                    value={monthlyBasePrice}
                    onChange={(e) => setMonthlyBasePrice(e.target.value)}
                    inputMode="decimal"
                    placeholder="29.00"
                  />
                </Field>
                <Field id="pf-interval" label={t("planForm.field.interval")} required>
                  <Select<PlanInterval>
                    id="pf-interval"
                    value={interval}
                    onValueChange={(v) => setInterval(v === "Yearly" ? "Yearly" : "Monthly")}
                    options={INTERVAL_OPTIONS}
                  />
                </Field>
                {interval === "Yearly" && (
                  <Field
                    id="pf-annualPrice"
                    label={t("planForm.field.annualPrice")}
                    hint={t("planForm.field.annualPriceHint")}
                    error={annualError ? t(annualError) : undefined}
                  >
                    <Input
                      id="pf-annualPrice"
                      value={annualPrice}
                      onChange={(e) => setAnnualPrice(e.target.value)}
                      inputMode="decimal"
                      placeholder={monthlyBasePrice ? String(Number(monthlyBasePrice) * 12) : "290.00"}
                    />
                  </Field>
                )}
              </div>
            </div>

            {/* ── Overage rates ── */}
            <div className="space-y-3">
              <SectionLabel
                icon={Gauge}
                title={t("planForm.section.overage.title")}
                description={t("planForm.section.overage.description")}
              />
              <div className="h-px bg-[var(--color-border)] opacity-60" />
              <div className="grid gap-4 sm:grid-cols-2">
                {OVERAGE_RESOURCES.map((res) => (
                  <Field
                    key={res.key}
                    id={`pf-overage-${res.key}`}
                    label={t(res.labelKey)}
                    error={overageErrors[res.key] ? t(overageErrors[res.key]!) : undefined}
                  >
                    <Input
                      id={`pf-overage-${res.key}`}
                      value={overage[res.key] ?? ""}
                      onChange={(e) => setOverage((s) => ({ ...s, [res.key]: e.target.value }))}
                      inputMode="decimal"
                      placeholder={res.placeholder}
                    />
                  </Field>
                ))}
              </div>
            </div>
          </DialogBody>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={pending}>
              {t("planForm.cancel")}
            </Button>
            <Button type="submit" disabled={pending || keyInvalid || pricingInvalid}>
              {pending ? t("planForm.saving") : isEdit ? t("planForm.saveChanges") : t("planForm.createPlan")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

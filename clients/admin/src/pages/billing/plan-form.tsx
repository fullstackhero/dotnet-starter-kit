import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft } from "lucide-react";
import { toast } from "sonner";
import {
  createPlan,
  getPlans,
  updatePlan,
  type BillingPlanDto,
  type QuotaResource,
} from "@/api/billing";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiRequestError } from "@/lib/api-client";

// Plan keys are canonical lowercase slugs (a-z 0-9 -), 2-64 chars.
const PLAN_KEY_PATTERN = /^[a-z0-9][a-z0-9-]{0,62}[a-z0-9]$/;

const OVERAGE_RESOURCES: { key: QuotaResource; label: string; placeholder: string }[] = [
  { key: "ApiCalls", label: "API calls", placeholder: "0.0010" },
  { key: "StorageBytes", label: "Storage bytes", placeholder: "0.00000001" },
  { key: "Users", label: "Users", placeholder: "5.00" },
  { key: "ActiveFeatureFlags", label: "Feature flags", placeholder: "1.00" },
];

type OverageState = Record<string, string>;

function toOverageNumbers(state: OverageState): Record<string, number> | null {
  const out: Record<string, number> = {};
  let any = false;
  for (const { key } of OVERAGE_RESOURCES) {
    const raw = state[key];
    if (raw === undefined || raw.trim() === "") continue;
    const n = Number(raw);
    if (!Number.isFinite(n) || n < 0) continue;
    out[key] = n;
    any = true;
  }
  return any ? out : null;
}

function describe(err: unknown, fallback: string): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return fallback;
}

export function PlanFormPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { planId } = useParams<{ planId?: string }>();
  const isEdit = !!planId;

  // ── form state ─────────────────────────────────────────────────────
  const [key, setKey] = useState("");
  const [name, setName] = useState("");
  const [currency, setCurrency] = useState("USD");
  const [monthlyBasePrice, setMonthlyBasePrice] = useState("");
  const [overage, setOverage] = useState<OverageState>({});

  // ── load existing plan when editing ────────────────────────────────
  // Include inactive plans so editing a deactivated plan still works.
  const plansQuery = useQuery({
    queryKey: ["billing", "plans", { includeInactive: true }],
    queryFn: () => getPlans(true),
    enabled: isEdit,
  });
  const existing = useMemo<BillingPlanDto | undefined>(() => {
    if (!isEdit || !plansQuery.data) return undefined;
    return plansQuery.data.find((p) => p.id === planId);
  }, [isEdit, plansQuery.data, planId]);

  useEffect(() => {
    if (!existing) return;
    setKey(existing.key);
    setName(existing.name);
    setCurrency(existing.currency);
    setMonthlyBasePrice(String(existing.monthlyBasePrice));
    const next: OverageState = {};
    for (const [resource, rate] of Object.entries(existing.overageRates)) {
      if (rate !== undefined && rate !== null) next[resource] = String(rate);
    }
    setOverage(next);
  }, [existing]);

  // ── validation ─────────────────────────────────────────────────────
  const keyInvalid = !isEdit && key.length > 0 && !PLAN_KEY_PATTERN.test(key);
  const priceNum = Number(monthlyBasePrice);
  const priceInvalid =
    monthlyBasePrice.length > 0 && (!Number.isFinite(priceNum) || priceNum < 0);

  // ── submit ─────────────────────────────────────────────────────────
  const createMutation = useMutation({
    mutationFn: createPlan,
    onSuccess: () => {
      toast.success(`Plan "${name}" created`);
      queryClient.invalidateQueries({ queryKey: ["billing", "plans"] });
      navigate("/billing/plans");
    },
    onError: (err) => toast.error("Create failed", { description: describe(err, "Could not create plan.") }),
  });

  const updateMutation = useMutation({
    mutationFn: updatePlan,
    onSuccess: () => {
      toast.success(`Plan "${name}" updated`);
      queryClient.invalidateQueries({ queryKey: ["billing", "plans"] });
      navigate("/billing/plans");
    },
    onError: (err) => toast.error("Update failed", { description: describe(err, "Could not update plan.") }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (priceInvalid) return;

    const overageRates = toOverageNumbers(overage);

    if (isEdit && planId) {
      updateMutation.mutate({
        planId,
        name: name.trim(),
        monthlyBasePrice: priceNum,
        overageRates,
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
    });
  };

  const pending = createMutation.isPending || updateMutation.isPending;
  const loadingExisting = isEdit && plansQuery.isLoading;

  return (
    <div className="space-y-6">
      <div>
        <Button variant="ghost" size="sm" onClick={() => navigate("/billing/plans")} className="-ml-2 mb-2">
          <ArrowLeft className="mr-1 h-4 w-4" /> All plans
        </Button>
        <h2 className="font-display text-2xl font-semibold tracking-tight">
          {isEdit ? `Edit plan` : "New plan"}
        </h2>
        <p className="text-sm text-[var(--color-muted-foreground)]">
          {isEdit
            ? "Update name, base price, or overage rates. Key and currency are immutable."
            : "Plan keys are canonical slugs used by tenant subscriptions and quota configuration."}
        </p>
      </div>

      <Card className="max-w-2xl">
        <form onSubmit={onSubmit}>
          <CardHeader>
            <CardTitle>Plan details</CardTitle>
            <CardDescription>
              {isEdit
                ? loadingExisting
                  ? "Loading current plan…"
                  : existing
                    ? `Currently ${existing.isActive ? "active" : "inactive"}.`
                    : "Plan not found."
                : "Created plans are active by default."}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Field
              id="key"
              label="Key"
              hint="Lowercase letters, digits, and hyphens. Used as a stable identifier (e.g. 'pro', 'team-2025')."
              value={key}
              onChange={setKey}
              placeholder="pro"
              required={!isEdit}
              error={keyInvalid ? "Invalid slug." : undefined}
              disabled={isEdit}
              autoComplete="off"
            />
            <Field
              id="name"
              label="Display name"
              value={name}
              onChange={setName}
              required
              placeholder="Pro"
            />
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-[1fr_1fr]">
              <Field
                id="currency"
                label="Currency"
                hint="ISO 4217 code. Locked after creation."
                value={currency}
                onChange={(v) => setCurrency(v.toUpperCase())}
                required={!isEdit}
                disabled={isEdit}
                placeholder="USD"
                autoComplete="off"
              />
              <Field
                id="monthlyBasePrice"
                label="Monthly base price"
                hint="Recurring fee charged each billing period."
                value={monthlyBasePrice}
                onChange={setMonthlyBasePrice}
                inputMode="decimal"
                required
                placeholder="29.00"
                error={priceInvalid ? "Must be a non-negative number." : undefined}
              />
            </div>

            <div className="mt-2 space-y-3 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-4">
              <div className="flex items-baseline justify-between">
                <div>
                  <h3 className="text-sm font-medium">Overage rates</h3>
                  <p className="text-xs text-[var(--color-muted-foreground)]">
                    Per-unit price when a tenant exceeds the plan limit. Leave blank to skip a resource.
                  </p>
                </div>
                <span className="font-mono text-[10.5px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                  optional
                </span>
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                {OVERAGE_RESOURCES.map((res) => (
                  <Field
                    key={res.key}
                    id={`overage-${res.key}`}
                    label={res.label}
                    value={overage[res.key] ?? ""}
                    onChange={(v) => setOverage((s) => ({ ...s, [res.key]: v }))}
                    inputMode="decimal"
                    placeholder={res.placeholder}
                  />
                ))}
              </div>
            </div>
          </CardContent>
          <CardFooter className="gap-2">
            <Button type="submit" disabled={pending || loadingExisting || keyInvalid || priceInvalid}>
              {pending ? "Saving…" : isEdit ? "Save changes" : "Create plan"}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => navigate("/billing/plans")}
              disabled={pending}
            >
              Cancel
            </Button>
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}

// ─── local field primitive (mirrors clients/admin/src/pages/tenants/create.tsx) ─

type FieldProps = {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  inputMode?: React.HTMLAttributes<HTMLInputElement>["inputMode"];
  required?: boolean;
  hint?: string;
  error?: string;
  placeholder?: string;
  autoComplete?: string;
  disabled?: boolean;
};

function Field({
  id,
  label,
  value,
  onChange,
  type,
  inputMode,
  required,
  hint,
  error,
  placeholder,
  autoComplete,
  disabled,
}: FieldProps) {
  return (
    <div className="space-y-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input
        id={id}
        type={type ?? "text"}
        inputMode={inputMode}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        placeholder={placeholder}
        autoComplete={autoComplete}
        disabled={disabled}
        aria-invalid={error ? true : undefined}
      />
      {hint && <p className="text-xs text-[var(--color-muted-foreground)]">{hint}</p>}
      {error && <p className="text-xs text-[var(--color-destructive)]">{error}</p>}
    </div>
  );
}

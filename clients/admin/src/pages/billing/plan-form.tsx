import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, CreditCard } from "lucide-react";
import { toast } from "sonner";
import {
  createPlan,
  getPlans,
  updatePlan,
  type BillingPlanDto,
  type QuotaResource,
} from "@/api/billing";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { EntityPageHeader, SettingsSection, Field } from "@/components/list";
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
        <Button variant="ghost" size="sm" onClick={() => navigate("/billing/plans")} className="-ml-2 mb-4">
          <ArrowLeft className="mr-1 h-4 w-4" /> All plans
        </Button>
        <EntityPageHeader
          icon={CreditCard}
          tone="saffron"
          title={isEdit ? "Edit plan" : "New plan"}
          description={
            isEdit
              ? "Update name, base price, or overage rates. Key and currency are immutable."
              : "Plan keys are canonical slugs used by tenant subscriptions and quota configuration."
          }
        />
      </div>

      <form onSubmit={onSubmit} className="max-w-2xl space-y-4">
        <SettingsSection
          icon={CreditCard}
          title="Plan details"
          description={
            isEdit
              ? loadingExisting
                ? "Loading current plan…"
                : existing
                  ? `Currently ${existing.isActive ? "active" : "inactive"}.`
                  : "Plan not found."
              : "Created plans are active by default."
          }
          footer={
            <div className="flex gap-2">
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
            </div>
          }
        >
          <div className="space-y-4">
            <Field
              id="key"
              label="Key"
              hint="Lowercase letters, digits, and hyphens. Used as a stable identifier (e.g. 'pro', 'team-2025')."
              required={!isEdit}
              error={keyInvalid ? "Invalid slug." : undefined}
            >
              <Input
                id="key"
                value={key}
                onChange={(e) => setKey(e.target.value)}
                placeholder="pro"
                required={!isEdit}
                disabled={isEdit}
                autoComplete="off"
              />
            </Field>
            <Field id="name" label="Display name" required>
              <Input
                id="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                placeholder="Pro"
              />
            </Field>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-[1fr_1fr]">
              <Field
                id="currency"
                label="Currency"
                hint="ISO 4217 code. Locked after creation."
                required={!isEdit}
              >
                <Input
                  id="currency"
                  value={currency}
                  onChange={(e) => setCurrency(e.target.value.toUpperCase())}
                  required={!isEdit}
                  disabled={isEdit}
                  placeholder="USD"
                  autoComplete="off"
                />
              </Field>
              <Field
                id="monthlyBasePrice"
                label="Monthly base price"
                hint="Recurring fee charged each billing period."
                required
                error={priceInvalid ? "Must be a non-negative number." : undefined}
              >
                <Input
                  id="monthlyBasePrice"
                  value={monthlyBasePrice}
                  onChange={(e) => setMonthlyBasePrice(e.target.value)}
                  inputMode="decimal"
                  required
                  placeholder="29.00"
                />
              </Field>
            </div>

            <SettingsSection
              title="Overage rates"
              description="Per-unit price when a tenant exceeds the plan limit. Leave blank to skip a resource."
            >
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                {OVERAGE_RESOURCES.map((res) => (
                  <Field key={res.key} id={`overage-${res.key}`} label={res.label}>
                    <Input
                      id={`overage-${res.key}`}
                      value={overage[res.key] ?? ""}
                      onChange={(e) => setOverage((s) => ({ ...s, [res.key]: e.target.value }))}
                      inputMode="decimal"
                      placeholder={res.placeholder}
                    />
                  </Field>
                ))}
              </div>
            </SettingsSection>
          </div>
        </SettingsSection>
      </form>
    </div>
  );
}

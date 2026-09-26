import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarClock } from "lucide-react";
import { toast } from "sonner";
import { renewTenant } from "@/api/tenants";
import { getPlans, planTermPrice } from "@/api/billing";
import { Button } from "@/components/ui/button";
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
import { formatCurrency, formatDate } from "@/lib/format";

/**
 * Renew or change a tenant's plan. Renewing the same plan extends validity by one term; choosing a
 * different plan switches the tenant from the renewal forward. The server issues the term invoice.
 */
export function RenewTenantDialog({
  open,
  onOpenChange,
  tenantId,
  currentPlanKey,
  validUpto,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  tenantId: string;
  currentPlanKey?: string | null;
  validUpto?: string;
}) {
  const { t } = useTranslation("tenants");
  const queryClient = useQueryClient();
  const [planKey, setPlanKey] = useState<string>("");

  const plansQuery = useQuery({
    queryKey: ["billing", "plans", "active"],
    queryFn: () => getPlans(false),
    enabled: open,
  });

  // Default the selection to the tenant's current plan once plans (and the current key) are known.
  useEffect(() => {
    if (!open) return;
    if (currentPlanKey && !planKey) setPlanKey(currentPlanKey);
  }, [open, currentPlanKey, planKey]);

  const options: SelectOption[] = (plansQuery.data ?? []).map((p) => ({
    value: p.key,
    label: p.key === currentPlanKey ? t("renew.planCurrent", { name: p.name }) : p.name,
    hint: `${p.interval} · ${formatCurrency(planTermPrice(p), p.currency)}`,
  }));

  const mutation = useMutation({
    mutationFn: (key: string) => renewTenant(tenantId, key || null),
    onSuccess: (result) => {
      toast.success(
        result.planChanged ? t("renew.toast.planChanged", { plan: result.planKey }) : t("renew.toast.renewed"),
        { description: t("renew.toast.description", { date: formatDate(result.validUpto) }) },
      );
      queryClient.invalidateQueries({ queryKey: ["tenant", tenantId] });
      queryClient.invalidateQueries({ queryKey: ["tenants"] });
      onOpenChange(false);
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error(t("renew.toast.failed"), { description: detail });
    },
  });

  const planChanged = !!planKey && planKey !== currentPlanKey;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent size="md">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="grid h-9 w-9 shrink-0 place-items-center rounded-xl
                bg-[oklch(from_var(--color-primary)_l_c_h_/_0.12)] text-[var(--color-primary)]
                ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]"
            >
              <CalendarClock className="h-[18px] w-[18px]" />
            </span>
            <DialogTitle className="text-[16px]">{t("renew.title")}</DialogTitle>
          </div>
          <DialogDescription className="mt-1">
            {t("renew.description", { date: formatDate(validUpto) })}
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="space-y-4">
          <Field
            id="renew-plan"
            label={t("renew.field.plan")}
            required
            hint={
              planChanged
                ? t("renew.field.hintSwitching")
                : t("renew.field.hintRenewing")
            }
          >
            <Select
              id="renew-plan"
              value={planKey}
              onValueChange={setPlanKey}
              options={options}
              emptyLabel={plansQuery.isLoading ? t("renew.plan.loading") : options.length === 0 ? t("renew.plan.none") : undefined}
              disabled={plansQuery.isLoading || options.length === 0}
            />
          </Field>
        </DialogBody>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
            {t("renew.cancel")}
          </Button>
          <Button type="button" onClick={() => mutation.mutate(planKey)} disabled={mutation.isPending || !planKey}>
            {mutation.isPending ? t("renew.renewing") : planChanged ? t("renew.changeAndRenew") : t("renew.submit")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

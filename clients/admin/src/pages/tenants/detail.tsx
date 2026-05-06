import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import {
  changeTenantActivation,
  getTenantProvisioningStatus,
  getTenantStatus,
  retryTenantProvisioning,
  type TenantProvisioningStep,
} from "@/api/tenants";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiRequestError } from "@/lib/api-client";

export function TenantDetailPage() {
  const { id = "" } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const tenantQuery = useQuery({
    queryKey: ["tenant", id],
    queryFn: () => getTenantStatus(id),
    enabled: !!id,
  });

  const provisioningQuery = useQuery({
    queryKey: ["tenant", id, "provisioning"],
    queryFn: () => getTenantProvisioningStatus(id),
    enabled: !!id,
    // Poll while provisioning is still in flight; back off once terminal.
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      if (status === "Completed" || status === "Failed") return false;
      return 2000;
    },
  });

  const activationMutation = useMutation({
    mutationFn: (isActive: boolean) => changeTenantActivation(id, isActive),
    onSuccess: (result) => {
      toast.success(result.isActive ? "Tenant activated" : "Tenant deactivated");
      queryClient.invalidateQueries({ queryKey: ["tenant", id] });
      queryClient.invalidateQueries({ queryKey: ["tenants"] });
    },
    onError: (err) => toast.error("Activation change failed", { description: describe(err) }),
  });

  const retryMutation = useMutation({
    mutationFn: () => retryTenantProvisioning(id),
    onSuccess: () => {
      toast.success("Provisioning re-queued");
      queryClient.invalidateQueries({ queryKey: ["tenant", id, "provisioning"] });
    },
    onError: (err) => toast.error("Retry failed", { description: describe(err) }),
  });

  const tenant = tenantQuery.data;
  const provisioning = provisioningQuery.data;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <Button variant="ghost" size="sm" onClick={() => navigate("/tenants")} className="mb-2 -ml-2">
            <ArrowLeft className="mr-1 h-4 w-4" /> All tenants
          </Button>
          <h1 className="text-2xl font-semibold tracking-tight">
            {tenant?.name ?? id}
          </h1>
          <p className="text-sm text-[var(--color-muted-foreground)] font-mono">{id}</p>
        </div>
        {tenant && (
          <Button
            variant={tenant.isActive ? "outline" : "default"}
            onClick={() => activationMutation.mutate(!tenant.isActive)}
            disabled={activationMutation.isPending}
          >
            {activationMutation.isPending
              ? "Updating…"
              : tenant.isActive
                ? "Deactivate"
                : "Activate"}
          </Button>
        )}
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Details</CardTitle>
            <CardDescription>Identity and lifecycle metadata.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3 text-sm">
            {tenantQuery.isLoading && <span className="text-[var(--color-muted-foreground)]">Loading…</span>}
            {tenantQuery.isError && (
              <span className="text-[var(--color-destructive)]">{describe(tenantQuery.error)}</span>
            )}
            {tenant && (
              <>
                <Detail label="Status" value={tenant.isActive ? "Active" : "Inactive"} />
                <Detail label="Admin email" value={tenant.adminEmail} />
                <Detail label="Issuer" value={tenant.issuer ?? "—"} />
                <Detail label="Valid until" value={formatDate(tenant.validUpto)} />
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-start justify-between space-y-0">
            <div>
              <CardTitle>Provisioning</CardTitle>
              <CardDescription>
                {provisioning ? (
                  <ProvisioningSummary provisioning={provisioning} />
                ) : (
                  "Loading provisioning status…"
                )}
              </CardDescription>
            </div>
            {provisioning?.status === "Failed" && (
              <Button
                size="sm"
                variant="outline"
                onClick={() => retryMutation.mutate()}
                disabled={retryMutation.isPending}
              >
                <RefreshCw className="mr-1 h-3.5 w-3.5" /> Retry
              </Button>
            )}
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            {provisioningQuery.isError && (
              <span className="text-[var(--color-destructive)]">{describe(provisioningQuery.error)}</span>
            )}
            {provisioning?.steps.map((step) => <StepRow key={step.step} step={step} />)}
            {provisioning?.error && (
              <pre className="mt-3 max-h-40 overflow-auto rounded-md bg-[var(--color-muted)] p-2 text-xs text-[var(--color-destructive)]">
                {provisioning.error}
              </pre>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4">
      <span className="text-[var(--color-muted-foreground)]">{label}</span>
      <span className="font-medium text-right">{value}</span>
    </div>
  );
}

function StepRow({ step }: { step: TenantProvisioningStep }) {
  return (
    <div className="flex items-center justify-between border-b border-[var(--color-border)] py-1.5 last:border-b-0">
      <span className="font-mono text-xs">{step.step}</span>
      <StatusBadge status={step.status} />
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const colors =
    status === "Completed"
      ? "bg-emerald-500/15 text-emerald-500"
      : status === "Failed"
        ? "bg-red-500/15 text-red-500"
        : status === "Running"
          ? "bg-blue-500/15 text-blue-500"
          : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]";
  return <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${colors}`}>{status}</span>;
}

function ProvisioningSummary({ provisioning }: { provisioning: { status: string; currentStep?: string | null } }) {
  if (provisioning.status === "Completed") return <span>Completed.</span>;
  if (provisioning.status === "Failed") return <span>Failed at {provisioning.currentStep ?? "unknown step"}.</span>;
  return <span>{provisioning.status}{provisioning.currentStep ? ` — ${provisioning.currentStep}` : ""}…</span>;
}

function formatDate(value: string | undefined): string {
  if (!value) return "—";
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString();
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return String(err);
}

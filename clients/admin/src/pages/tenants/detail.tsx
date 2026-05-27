import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  Building2,
  CheckCircle2,
  CircleDashed,
  ClipboardList,
  Info,
  Loader2,
  RefreshCw,
  UserCog,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/auth/use-auth";
import { ImpersonateDialog } from "@/components/impersonation/impersonate-dialog";
import { ActiveGrantsCard } from "@/components/impersonation/active-grants-card";
import { TenantBrandingCard } from "@/components/tenants/tenant-branding-card";
import { IdentityPermissions } from "@/lib/permissions";
import {
  changeTenantActivation,
  getTenantProvisioningStatus,
  getTenantStatus,
  retryTenantProvisioning,
  type TenantProvisioningStep,
} from "@/api/tenants";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Monogram } from "@/components/monogram";
import {
  EntityPageHeader,
  ErrorBand,
  LoadingRow,
  SettingsSection,
} from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

export function TenantDetailPage() {
  const { id = "" } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user: currentUser } = useAuth();
  const [impersonateOpen, setImpersonateOpen] = useState(false);
  const canImpersonate = (currentUser?.permissions ?? []).includes(
    IdentityPermissions.Users.Impersonate,
  );

  const tenantQuery = useQuery({
    queryKey: ["tenant", id],
    queryFn: () => getTenantStatus(id),
    enabled: !!id,
  });

  const provisioningQuery = useQuery({
    queryKey: ["tenant", id, "provisioning"],
    queryFn: () => getTenantProvisioningStatus(id),
    enabled: !!id,
    // A 404 means this tenant was never run through the provisioning pipeline
    // (e.g. demo/directly-created tenants) — a terminal "not tracked" state,
    // not a transient failure. Don't retry or poll it.
    retry: (failureCount, err) =>
      !(err instanceof ApiRequestError && err.status === 404) && failureCount < 3,
    // Poll while provisioning is in flight; stop once terminal (or not tracked).
    refetchInterval: (query) => {
      if (query.state.error instanceof ApiRequestError && query.state.error.status === 404) {
        return false;
      }
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
  const provisioningNotTracked =
    provisioningQuery.error instanceof ApiRequestError &&
    provisioningQuery.error.status === 404;

  return (
    <div className="space-y-8">
      <EntityPageHeader
        icon={Building2}
        title={tenant?.name ?? "Tenant"}
        tone="info"
        description={tenant?.adminEmail}
      >
        <Button variant="ghost" size="sm" onClick={() => navigate("/tenants")}>
          <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Registry
        </Button>
      </EntityPageHeader>

      {tenantQuery.isError && (
        <ErrorBand message={describe(tenantQuery.error)} />
      )}

      {tenantQuery.isLoading && !tenant && <LoadingRow label="Loading tenant" />}

      {tenant && (
        <>
          {/* Hero identity card */}
          <SettingsSection
            title="Overview"
            icon={Building2}
          >
            <div className="flex flex-col items-start gap-6 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex items-center gap-5">
                <Monogram seed={tenant.id} fallback={tenant.name} size="lg" />
                <div>
                  <h2 className="font-display text-2xl font-semibold tracking-tight md:text-3xl">
                    {tenant.name}
                  </h2>
                  <div className="mt-1 flex flex-wrap items-baseline gap-x-4 gap-y-1 font-mono text-xs text-[var(--color-muted-foreground)]">
                    <code className="code-chip">{tenant.id}</code>
                    <span className="truncate">{tenant.adminEmail}</span>
                  </div>
                  <div className="mt-3 flex flex-wrap items-center gap-2">
                    <Badge variant={tenant.isActive ? "success" : "muted"} className="font-mono uppercase tracking-[0.14em]">
                      {tenant.isActive ? "Active" : "Inactive"}
                    </Badge>
                    <Badge variant="outline" className="font-mono uppercase tracking-[0.14em]">
                      valid · {formatDate(tenant.validUpto)}
                    </Badge>
                    {tenant.issuer && (
                      <Badge variant="outline" className="font-mono uppercase tracking-[0.14em]">
                        iss · {tenant.issuer}
                      </Badge>
                    )}
                  </div>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-2">
                {canImpersonate && tenant.isActive && (
                  <Button
                    variant="signal"
                    onClick={() => setImpersonateOpen(true)}
                    className="shrink-0"
                    title="Sign in as a user inside this tenant"
                  >
                    <UserCog className="mr-1.5 h-3.5 w-3.5" />
                    Impersonate user
                  </Button>
                )}
                <Button
                  variant={tenant.isActive ? "outline" : "default"}
                  onClick={() => activationMutation.mutate(!tenant.isActive)}
                  disabled={activationMutation.isPending}
                  className="shrink-0"
                >
                  {activationMutation.isPending
                    ? "Updating…"
                    : tenant.isActive
                      ? "Deactivate tenant"
                      : "Activate tenant"}
                </Button>
              </div>
            </div>
          </SettingsSection>

          <ImpersonateDialog
            open={impersonateOpen}
            onOpenChange={setImpersonateOpen}
            tenantId={tenant.id}
            tenantName={tenant.name}
          />

          <ActiveGrantsCard tenantId={tenant.id} />

          <TenantBrandingCard tenantId={tenant.id} />

          {/* Details section */}
          <SettingsSection
            title="Details"
            icon={Info}
            description="The tenant's identity, contact, and subscription window. Identifiers are immutable; the issuer is used to scope JWTs."
          >
            <dl className="divide-y divide-[var(--color-border)]">
              <DetailRow label="Identifier" mono>{tenant.id}</DetailRow>
              <DetailRow label="Name">{tenant.name}</DetailRow>
              <DetailRow label="Admin email" mono>{tenant.adminEmail}</DetailRow>
              <DetailRow label="JWT issuer" mono>{tenant.issuer ?? "—"}</DetailRow>
              <DetailRow label="Valid until">{formatDate(tenant.validUpto)}</DetailRow>
              <DetailRow label="Status">{tenant.isActive ? "Active" : "Inactive"}</DetailRow>
            </dl>
          </SettingsSection>

          {/* Provisioning section */}
          <SettingsSection
            title="Provisioning"
            icon={ClipboardList}
            description="Live status of the background pipeline that seeds the tenant database, default roles, and admin user. Polls every 2 seconds while running."
          >
            <ProvisioningPanel
              steps={provisioning?.steps ?? []}
              status={provisioning?.status}
              currentStep={provisioning?.currentStep ?? undefined}
              errorBody={provisioning?.error ?? undefined}
              loading={provisioningQuery.isLoading}
              // A 404 isn't an error to surface — the tenant was just never run
              // through the pipeline. Swallow it and show the neutral state.
              error={provisioningNotTracked ? undefined : provisioningQuery.error}
              notTracked={provisioningNotTracked}
              onRetry={() => retryMutation.mutate()}
              retryPending={retryMutation.isPending}
            />
          </SettingsSection>
        </>
      )}
    </div>
  );
}

// ─── subcomponents ──────────────────────────────────────────────────────

function DetailRow({
  label,
  children,
  mono,
}: {
  label: string;
  children: React.ReactNode;
  mono?: boolean;
}) {
  return (
    <div className="grid grid-cols-[10rem_1fr] items-baseline gap-4 py-2.5">
      <dt className="meta text-[var(--color-muted-foreground)]">{label}</dt>
      <dd className={cn("min-w-0 break-words text-sm", mono && "font-mono text-[0.8125rem]")}>
        {children}
      </dd>
    </div>
  );
}

function ProvisioningPanel({
  steps,
  status,
  currentStep,
  errorBody,
  loading,
  error,
  notTracked = false,
  onRetry,
  retryPending,
}: {
  steps: TenantProvisioningStep[];
  status?: string;
  currentStep?: string;
  errorBody?: string;
  loading: boolean;
  error: unknown;
  notTracked?: boolean;
  onRetry: () => void;
  retryPending: boolean;
}) {
  const overall = notTracked ? "Not tracked" : status ?? (loading ? "Loading" : "Unknown");
  const overallVariant =
    status === "Completed"
      ? "success"
      : status === "Failed"
        ? "danger"
        : status === "Running"
          ? "info"
          : "outline";

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Badge variant={overallVariant} className="font-mono uppercase tracking-[0.14em]">
          {status === "Failed"
            ? `Failed at ${currentStep ?? "unknown step"}`
            : currentStep
              ? `${overall} · ${currentStep}`
              : overall}
        </Badge>
        {status === "Failed" && (
          <Button size="sm" variant="outline" onClick={onRetry} disabled={retryPending}>
            <RefreshCw className={cn("mr-1 h-3.5 w-3.5", retryPending && "animate-spin")} />
            {retryPending ? "Re-queuing…" : "Retry provisioning"}
          </Button>
        )}
      </div>

      {error ? (
        <ErrorBand message={describe(error)} />
      ) : loading && steps.length === 0 ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Loading…</p>
      ) : notTracked ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">
          This tenant wasn't created through the provisioning pipeline, so there's no run
          history to show. Tenants created via the console report their seed/migrate steps here.
        </p>
      ) : steps.length === 0 ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">
          No provisioning runs recorded.
        </p>
      ) : (
        <ol className="divide-y divide-[var(--color-border)] border-y border-[var(--color-border)]">
          {steps.map((step, i) => (
            <StepRow key={step.step} step={step} index={i + 1} />
          ))}
        </ol>
      )}

      {errorBody && (
        <pre className="max-h-44 overflow-auto rounded-md border border-[var(--color-destructive)]/40 bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] p-3 font-mono text-[11px] whitespace-pre-wrap text-[var(--color-destructive)]">
          {errorBody}
        </pre>
      )}
    </div>
  );
}

function StepRow({ step, index }: { step: TenantProvisioningStep; index: number }) {
  const tone =
    step.status === "Completed"
      ? "text-[var(--color-success)]"
      : step.status === "Failed"
        ? "text-[var(--color-destructive)]"
        : step.status === "Running"
          ? "text-[var(--color-info)]"
          : "text-[var(--color-muted-foreground)]";
  const Icon =
    step.status === "Completed"
      ? CheckCircle2
      : step.status === "Failed"
        ? XCircle
        : step.status === "Running"
          ? Loader2
          : CircleDashed;

  const duration =
    step.startedUtc && step.completedUtc
      ? formatDuration(step.startedUtc, step.completedUtc)
      : step.startedUtc
        ? "in flight"
        : null;

  return (
    <li className="grid grid-cols-[2rem_auto_1fr_auto_auto] items-center gap-3 py-2.5">
      <span className="font-mono text-[10.5px] tabular-nums text-[var(--color-muted-foreground)]">
        {String(index).padStart(2, "0")}
      </span>
      <Icon className={cn("h-4 w-4", tone, step.status === "Running" && "animate-spin")} />
      <span className="truncate font-mono text-[13px]">{step.step}</span>
      {duration && (
        <span className="font-mono text-[10.5px] tabular-nums text-[var(--color-muted-foreground)]">
          {duration}
        </span>
      )}
      <span className={cn("meta", tone)}>{step.status}</span>
    </li>
  );
}

// ─── helpers ────────────────────────────────────────────────────────────

function formatDate(value: string | undefined): string {
  if (!value) return "—";
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleDateString();
}

function formatDuration(start: string, end: string): string {
  const a = new Date(start).getTime();
  const b = new Date(end).getTime();
  if (Number.isNaN(a) || Number.isNaN(b)) return "—";
  const ms = Math.max(0, b - a);
  if (ms < 1000) return `${ms}ms`;
  const s = ms / 1000;
  if (s < 60) return `${s.toFixed(1)}s`;
  const m = Math.floor(s / 60);
  const rem = Math.round(s - m * 60);
  return `${m}m ${rem}s`;
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return String(err);
}

import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  Building2,
  CalendarClock,
  CalendarCog,
  CheckCircle2,
  CircleDashed,
  ClipboardList,
  CreditCard,
  Info,
  KeyRound,
  Loader2,
  Mail,
  RefreshCw,
  ServerCrash,
  UserCog,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/auth/use-auth";
import { ImpersonateDialog } from "@/components/impersonation/impersonate-dialog";
import { ActiveGrantsCard } from "@/components/impersonation/active-grants-card";
import { TenantBrandingCard } from "@/components/tenants/tenant-branding-card";
import { RenewTenantDialog } from "@/components/tenants/renew-tenant-dialog";
import { AdjustValidityDialog } from "@/components/tenants/adjust-validity-dialog";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { IdentityPermissions, MultitenancyPermissions } from "@/lib/permissions";
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
  const [renewOpen, setRenewOpen] = useState(false);
  const [adjustOpen, setAdjustOpen] = useState(false);
  const [activationConfirmOpen, setActivationConfirmOpen] = useState(false);
  const permissions = currentUser?.permissions ?? [];
  const canImpersonate = permissions.includes(IdentityPermissions.Users.Impersonate);
  // Renew / change plan + adjust validity are root-operator subscription actions.
  const canManageSubscription = permissions.includes(
    MultitenancyPermissions.Tenants.UpgradeSubscription,
  );
  // Activation toggle + retry-provisioning are tenant-update operations.
  const canUpdateTenant = permissions.includes(MultitenancyPermissions.Tenants.Update);

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
      setActivationConfirmOpen(false);
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
          {/* ── Hero identity card ─────────────────────────────────────── */}
          <SettingsSection title="Overview" icon={Building2}>
            <div className="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between">
              {/* Left: monogram + name + meta + badges */}
              <div className="flex items-start gap-4">
                <Monogram seed={tenant.id} fallback={tenant.name} size="lg" />
                <div className="min-w-0">
                  <h2 className="font-display text-2xl font-semibold tracking-tight md:text-3xl">
                    {tenant.name}
                  </h2>
                  <div className="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1">
                    <span className="inline-flex items-center gap-1.5 text-[12px] text-[var(--color-muted-foreground)]">
                      <Mail className="h-3 w-3 shrink-0" />
                      {tenant.adminEmail}
                    </span>
                    <span className="inline-flex items-center gap-1.5 font-mono text-[11px] text-[var(--color-muted-foreground)]">
                      <KeyRound className="h-3 w-3 shrink-0" />
                      <code>{tenant.id}</code>
                    </span>
                  </div>
                  <div className="mt-3 flex flex-wrap items-center gap-2">
                    <Badge variant={tenant.isActive ? "success" : "muted"}>
                      {tenant.isActive ? "Active" : "Inactive"}
                    </Badge>
                    {tenant.expiryState && tenant.expiryState !== "Active" && (
                      <Badge variant={expiryVariant(tenant.expiryState)}>
                        {tenant.expiryState === "InGrace" ? "In grace" : "Expired"}
                      </Badge>
                    )}
                    {tenant.plan && (
                      <Badge variant="outline">
                        <CreditCard className="h-3 w-3" />
                        {tenant.plan}
                      </Badge>
                    )}
                    <Badge variant="outline">
                      <CalendarClock className="h-3 w-3" />
                      Valid until {formatDate(tenant.validUpto)}
                    </Badge>
                    {tenant.issuer && (
                      <Badge variant="outline" className="font-mono text-[10.5px]">
                        iss · {tenant.issuer}
                      </Badge>
                    )}
                  </div>
                </div>
              </div>

              {/* Right: action buttons */}
              <div className="flex shrink-0 flex-wrap items-center gap-2">
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
                {canManageSubscription && (
                  <Button
                    variant="outline"
                    onClick={() => setRenewOpen(true)}
                    className="shrink-0"
                    title="Extend validity by one plan term, or switch plans"
                  >
                    <CalendarClock className="mr-1.5 h-3.5 w-3.5" />
                    Renew / change plan
                  </Button>
                )}
                {canManageSubscription && (
                  <Button
                    variant="outline"
                    onClick={() => setAdjustOpen(true)}
                    className="shrink-0"
                    title="Set the expiry date directly with no invoice (operator override)"
                  >
                    <CalendarCog className="mr-1.5 h-3.5 w-3.5" />
                    Adjust validity
                  </Button>
                )}
                {canUpdateTenant && (
                  <Button
                    variant={tenant.isActive ? "outline" : "default"}
                    onClick={() => setActivationConfirmOpen(true)}
                    disabled={activationMutation.isPending}
                    className="shrink-0"
                  >
                    {activationMutation.isPending
                      ? "Updating…"
                      : tenant.isActive
                        ? "Deactivate tenant"
                        : "Activate tenant"}
                  </Button>
                )}
              </div>
            </div>
          </SettingsSection>

          <ImpersonateDialog
            open={impersonateOpen}
            onOpenChange={setImpersonateOpen}
            tenantId={tenant.id}
            tenantName={tenant.name}
          />

          {canManageSubscription && (
            <RenewTenantDialog
              open={renewOpen}
              onOpenChange={setRenewOpen}
              tenantId={tenant.id}
              currentPlanKey={tenant.plan}
              validUpto={tenant.validUpto}
            />
          )}

          {canManageSubscription && (
            <AdjustValidityDialog
              open={adjustOpen}
              onOpenChange={setAdjustOpen}
              tenantId={tenant.id}
              validUpto={tenant.validUpto}
            />
          )}

          {canUpdateTenant && (
          <ConfirmDialog
            open={activationConfirmOpen}
            onOpenChange={setActivationConfirmOpen}
            destructive={tenant.isActive}
            title={tenant.isActive ? "Deactivate tenant?" : "Activate tenant?"}
            description={
              tenant.isActive ? (
                <>
                  Users of <strong className="text-[var(--color-foreground)]">{tenant.name}</strong> will
                  be blocked from signing in and all their API requests will be rejected until you
                  reactivate the tenant.
                </>
              ) : (
                <>
                  <strong className="text-[var(--color-foreground)]">{tenant.name}</strong>&apos;s users
                  will be able to sign in and use the platform again.
                </>
              )
            }
            confirmLabel={tenant.isActive ? "Deactivate" : "Activate"}
            pending={activationMutation.isPending}
            onConfirm={() => activationMutation.mutate(!tenant.isActive)}
          />
          )}

          <ActiveGrantsCard tenantId={tenant.id} />

          <TenantBrandingCard tenantId={tenant.id} />

          {/* ── Details section ────────────────────────────────────────── */}
          <SettingsSection
            title="Details"
            icon={Info}
            description="The tenant's identity, contact, and subscription window. Identifiers are immutable; the issuer scopes JWTs to this tenant."
          >
            <div className="space-y-0">
              <InfoRow label="Identifier" mono>{tenant.id}</InfoRow>
              <InfoRow label="Name">{tenant.name}</InfoRow>
              <InfoRow label="Admin email" mono>{tenant.adminEmail}</InfoRow>
              <InfoRow label="JWT issuer" mono>{tenant.issuer ?? "—"}</InfoRow>
              <InfoRow label="Plan">{tenant.plan ?? "—"}</InfoRow>
              <InfoRow label="Valid until">
                <span className="flex items-center gap-1.5">
                  <CalendarClock className="h-3.5 w-3.5 text-[var(--color-muted-foreground)]" />
                  {formatDate(tenant.validUpto)}
                  {tenant.expiryState && tenant.expiryState !== "Active" && (
                    <Badge variant={expiryVariant(tenant.expiryState)}>
                      {tenant.expiryState === "InGrace" ? "In grace" : "Expired"}
                    </Badge>
                  )}
                </span>
              </InfoRow>
              <InfoRow label="Status" isLast>
                <Badge variant={tenant.isActive ? "success" : "muted"}>
                  {tenant.isActive ? "Active" : "Inactive"}
                </Badge>
              </InfoRow>
            </div>
          </SettingsSection>

          {/* ── Provisioning section ───────────────────────────────────── */}
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
              canRetry={canUpdateTenant}
            />
          </SettingsSection>
        </>
      )}
    </div>
  );
}

// ─── subcomponents ──────────────────────────────────────────────────────────

/**
 * InfoRow — a single key/value row inside the Details card.
 * Matches the `ProfileRow` pattern from the dashboard's user-detail page:
 * label on the left (muted, small), value on the right (foreground, readable).
 * Separated by a subtle half-opacity border.
 */
function InfoRow({
  label,
  children,
  mono,
  isLast,
}: {
  label: string;
  children: React.ReactNode;
  mono?: boolean;
  isLast?: boolean;
}) {
  return (
    <div
      className={cn(
        "flex items-center justify-between gap-4 py-2.5",
        !isLast &&
          "border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)]",
      )}
    >
      <span className="shrink-0 text-[11.5px] font-medium text-[var(--color-muted-foreground)]">
        {label}
      </span>
      <span
        className={cn(
          "min-w-0 truncate text-right text-[13px] text-[var(--color-foreground)]",
          mono && "font-mono text-[12px]",
        )}
      >
        {children}
      </span>
    </div>
  );
}

/**
 * ProvisioningPanel — live pipeline status.
 * Renders a status badge + retry action, then either:
 *  - a "not tracked" neutral state (provisioningNotTracked)
 *  - a timeline-style step list
 *  - loading / empty / error states
 * The 404/notTracked logic is intentionally preserved verbatim.
 */
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
  canRetry = false,
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
  canRetry?: boolean;
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
      {/* Status bar */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2.5">
          <OverallStatusDot status={notTracked ? "NotTracked" : (status ?? "Unknown")} />
          <Badge variant={overallVariant}>
            {status === "Failed"
              ? `Failed at ${currentStep ?? "unknown step"}`
              : currentStep
                ? `${overall} · ${currentStep}`
                : overall}
          </Badge>
        </div>
        {status === "Failed" && canRetry && (
          <Button size="sm" variant="outline" onClick={onRetry} disabled={retryPending}>
            <RefreshCw className={cn("mr-1.5 h-3.5 w-3.5", retryPending && "animate-spin")} />
            {retryPending ? "Re-queuing…" : "Retry provisioning"}
          </Button>
        )}
      </div>

      {/* Body */}
      {error ? (
        <ErrorBand message={describe(error)} />
      ) : loading && steps.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">Loading…</p>
      ) : notTracked ? (
        <div className="flex items-start gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3.5">
          <ServerCrash
            aria-hidden
            className="mt-0.5 h-4 w-4 shrink-0 text-[var(--color-muted-foreground)]"
          />
          <p className="text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
            This tenant wasn't created through the provisioning pipeline, so there's no run
            history to show. Tenants created via the console report their seed/migrate steps here.
          </p>
        </div>
      ) : steps.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          No provisioning runs recorded.
        </p>
      ) : (
        <StepTimeline steps={steps} />
      )}

      {/* Error body (failed step detail) */}
      {errorBody && (
        <pre className="max-h-44 overflow-auto rounded-lg border border-[var(--color-destructive)]/40 bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] p-3.5 font-mono text-[11px] whitespace-pre-wrap text-[var(--color-destructive)]">
          {errorBody}
        </pre>
      )}
    </div>
  );
}

/**
 * OverallStatusDot — an animated pulsing dot that conveys overall
 * pipeline status at a glance alongside the badge text.
 */
function OverallStatusDot({ status }: { status: string }) {
  const color =
    status === "Completed"
      ? "bg-[var(--color-success)]"
      : status === "Failed"
        ? "bg-[var(--color-destructive)]"
        : status === "Running"
          ? "bg-[var(--color-info)]"
          : "bg-[var(--color-muted-foreground)]";

  const pulse = status === "Running";

  return (
    <span className="relative inline-flex h-2.5 w-2.5 shrink-0">
      {pulse && (
        <span
          className={cn(
            "absolute inline-flex h-full w-full animate-ping rounded-full opacity-60",
            color,
          )}
        />
      )}
      <span className={cn("relative inline-flex h-2.5 w-2.5 rounded-full", color)} />
    </span>
  );
}

/**
 * StepTimeline — renders provisioning steps as a connected vertical
 * timeline instead of a flat `<ol>` with dividers.
 * Each step has: a status icon track, step name, duration, and status label.
 */
function StepTimeline({ steps }: { steps: TenantProvisioningStep[] }) {
  return (
    <ol className="space-y-0">
      {steps.map((step, i) => (
        <StepRow key={step.step} step={step} index={i + 1} isLast={i === steps.length - 1} />
      ))}
    </ol>
  );
}

function StepRow({
  step,
  index,
  isLast,
}: {
  step: TenantProvisioningStep;
  index: number;
  isLast: boolean;
}) {
  const isCompleted = step.status === "Completed";
  const isFailed = step.status === "Failed";
  const isRunning = step.status === "Running";
  const isPending = !isCompleted && !isFailed && !isRunning;

  const iconColor = isCompleted
    ? "text-[var(--color-success)]"
    : isFailed
      ? "text-[var(--color-destructive)]"
      : isRunning
        ? "text-[var(--color-info)]"
        : "text-[var(--color-muted-foreground)]";

  const Icon = isCompleted
    ? CheckCircle2
    : isFailed
      ? XCircle
      : isRunning
        ? Loader2
        : CircleDashed;

  const trackColor = isCompleted
    ? "bg-[var(--color-success)]"
    : isFailed
      ? "bg-[var(--color-destructive)]"
      : isRunning
        ? "bg-[var(--color-info)]"
        : "bg-[var(--color-border-strong)]";

  const statusVariant = isCompleted
    ? "success"
    : isFailed
      ? "danger"
      : isRunning
        ? "info"
        : ("outline" as const);

  const duration =
    step.startedUtc && step.completedUtc
      ? formatDuration(step.startedUtc, step.completedUtc)
      : step.startedUtc
        ? "in flight"
        : null;

  return (
    <li className="flex items-stretch gap-3">
      {/* Timeline track column */}
      <div className="flex w-8 shrink-0 flex-col items-center">
        {/* Icon */}
        <span
          className={cn(
            "relative z-10 grid h-8 w-8 shrink-0 place-items-center rounded-full",
            "border border-[var(--color-border)] bg-[var(--color-card)]",
            iconColor,
          )}
        >
          <Icon
            className={cn("h-3.5 w-3.5", isRunning && "animate-spin")}
          />
        </span>
        {/* Vertical connector line — hidden on last item */}
        {!isLast && (
          <div
            className={cn("mt-1 w-[2px] flex-1 rounded-full opacity-30", trackColor)}
            style={{ minHeight: "1.25rem" }}
          />
        )}
      </div>

      {/* Content */}
      <div
        className={cn(
          "flex min-w-0 flex-1 flex-wrap items-center justify-between gap-x-4 gap-y-0.5 pb-4",
          isLast && "pb-0",
        )}
      >
        {/* Step index + name */}
        <div className="flex items-center gap-2 min-w-0">
          <span className="font-mono text-[10.5px] tabular-nums text-[var(--color-muted-foreground)] opacity-60">
            {String(index).padStart(2, "0")}
          </span>
          <span
            className={cn(
              "truncate font-mono text-[13px]",
              isPending
                ? "text-[var(--color-muted-foreground)]"
                : "text-[var(--color-foreground)]",
            )}
          >
            {step.step}
          </span>
        </div>

        {/* Duration + status */}
        <div className="flex shrink-0 items-center gap-2.5">
          {duration && (
            <span className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
              {duration}
            </span>
          )}
          <Badge variant={statusVariant} className="font-mono uppercase tracking-[0.12em]">
            {step.status}
          </Badge>
        </div>
      </div>
    </li>
  );
}

// ─── helpers ────────────────────────────────────────────────────────────────

function formatDate(value: string | undefined): string {
  if (!value) return "—";
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleDateString();
}

function expiryVariant(state: string): React.ComponentProps<typeof Badge>["variant"] {
  return state === "Expired" ? "danger" : state === "InGrace" ? "warning" : "outline";
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

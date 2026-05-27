import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown, Clock, RefreshCw, ShieldOff, UserCog } from "lucide-react";
import {
  listImpersonationGrants,
  type ImpersonationGrantDto,
  type ImpersonationGrantStatus,
} from "@/api/impersonation-grants";
import type { UserDto } from "@/api/users";
import { useAuth } from "@/auth/use-auth";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  EntityPageHeader,
  ErrorBand,
  LoadingRow,
  StatStrip,
  Stat,
  FilterBar,
} from "@/components/list";
import { Select } from "@/components/ui/select";
import { EmptyState } from "@/components/empty-state";
import { ImpersonateDialog } from "@/components/impersonation/impersonate-dialog";
import { RevokeGrantDialog } from "@/components/impersonation/revoke-grant-dialog";
import { IdentityPermissions } from "@/lib/permissions";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const REFRESH_INTERVAL_MS = 5_000;

const STATUS_OPTIONS: { value: ImpersonationGrantStatus; label: string }[] = [
  { value: "Active", label: "Active" },
  { value: "Ended", label: "Ended" },
  { value: "Revoked", label: "Revoked" },
  { value: "Expired", label: "Expired" },
];

export function ImpersonationListPage() {
  const { user } = useAuth();
  const canRevoke = (user?.permissions ?? []).includes(IdentityPermissions.Impersonation.Revoke);
  const canImpersonate = (user?.permissions ?? []).includes(IdentityPermissions.Users.Impersonate);
  const currentUserId = user?.id ?? null;

  const [status, setStatus] = useState<ImpersonationGrantStatus | "">("Active");
  const [targetGrant, setTargetGrant] = useState<ImpersonationGrantDto | null>(null);
  const [reopenGrant, setReopenGrant] = useState<ImpersonationGrantDto | null>(null);

  // One poll fetches all grants; the filtered list AND the KPI counts are both
  // derived client-side, so we don't run two overlapping take:200 polls.
  const grants = useQuery({
    queryKey: ["impersonation-grants", "all"],
    queryFn: () => listImpersonationGrants({ take: 200 }),
    // Poll while viewing — admins watching an active session expect near-real-time updates.
    refetchInterval: REFRESH_INTERVAL_MS,
    refetchOnWindowFocus: true,
  });

  const allGrants = useMemo(() => grants.data ?? [], [grants.data]);

  const counts = useMemo(
    () => ({
      active: allGrants.filter((g) => g.status === "Active").length,
      ended: allGrants.filter((g) => g.status === "Ended").length,
      revoked: allGrants.filter((g) => g.status === "Revoked").length,
      expired: allGrants.filter((g) => g.status === "Expired").length,
    }),
    [allGrants],
  );

  const items = useMemo(
    () => (status ? allGrants.filter((g) => g.status === status) : allGrants),
    [allGrants, status],
  );

  return (
    <div className="space-y-8">
      <EntityPageHeader
        icon={UserCog}
        tone="warning"
        title="Impersonation"
        description="Every impersonation token issued by the server is tracked here. Active grants can be revoked — the token is rejected by the JWT validation hook within seconds."
      >
        <Button
          variant="outline"
          size="sm"
          disabled={grants.isFetching}
          onClick={() => grants.refetch()}
          className="flex-1 sm:flex-none"
        >
          <RefreshCw className={cn("mr-1.5 h-3.5 w-3.5", grants.isFetching && "animate-spin")} />
          Refresh
        </Button>
      </EntityPageHeader>

      <StatStrip cols={4}>
        <Stat label="Active" value={grants.isLoading ? "—" : counts.active.toString()} hint="in-flight tokens" tone={counts.active > 0 ? "signal" : "default"} />
        <Stat label="Ended" value={grants.isLoading ? "—" : counts.ended.toString()} hint="operator clicked End" />
        <Stat label="Revoked" value={grants.isLoading ? "—" : counts.revoked.toString()} hint="forcibly invalidated" tone={counts.revoked > 0 ? "danger" : "default"} />
        <Stat label="Expired" value={grants.isLoading ? "—" : counts.expired.toString()} hint="reached natural TTL" />
      </StatStrip>

      <FilterBar>
        <Select
          value={status}
          onChange={(v) => setStatus(v as ImpersonationGrantStatus | "")}
          options={STATUS_OPTIONS}
          placeholder="All statuses"
          minWidth="12rem"
        />
      </FilterBar>

      {grants.isError && (
        <ErrorBand
          message={
            grants.error instanceof ApiRequestError
              ? grants.error.problem?.detail ?? grants.error.message
              : "Failed to load impersonation grants."
          }
        />
      )}

      {grants.isLoading && <LoadingRow label="Loading grants" />}

      {!grants.isLoading && items.length === 0 && !grants.isError && (
        <EmptyState
          icon={UserCog}
          kicker="// nothing here"
          title={status === "Active" ? "No active impersonations." : "No grants match this filter."}
          description={
            status === "Active"
              ? "When an operator starts impersonating a tenant user, the session appears here in real time."
              : "Try a different status filter to see the rest of the grant history."
          }
        />
      )}

      {items.length > 0 && (
        <ul className="divide-y divide-[var(--color-border)] border-y border-[var(--color-border)]">
          {items.map((g) => (
            <GrantRow
              key={g.id}
              grant={g}
              canRevoke={canRevoke && g.status === "Active"}
              // Re-open is only meaningful when:
              //   • the grant is still Active server-side (token not revoked / expired)
              //   • the caller is the original actor (this is the "I closed my
              //     browser tab" recovery path — not a way to hijack someone
              //     else's session)
              //   • the caller still has Users.Impersonate (revocation guard)
              canReopen={
                canImpersonate &&
                g.status === "Active" &&
                currentUserId !== null &&
                g.actorUserId === currentUserId
              }
              onRevoke={() => setTargetGrant(g)}
              onReopen={() => setReopenGrant(g)}
            />
          ))}
        </ul>
      )}

      <RevokeGrantDialog
        grant={targetGrant}
        onOpenChange={(open) => !open && setTargetGrant(null)}
      />

      <ReopenDialog
        grant={reopenGrant}
        onOpenChange={(open) => !open && setReopenGrant(null)}
      />
    </div>
  );
}

/**
 * ReopenDialog — adapter around ImpersonateDialog that pre-fills the
 * tenant + user from an existing Active grant. The dialog still requires
 * a fresh reason + duration; we don't reuse the prior grant's values
 * because the operator is starting a NEW audit-trail entry (the old
 * grant stays active until it expires or is revoked).
 *
 * UserDto is constructed locally from grant fields rather than fetched
 * by id — the dialog only needs (id, display name) for its picker step,
 * and the grant already carries both.
 */
function ReopenDialog({
  grant,
  onOpenChange,
}: {
  grant: ImpersonationGrantDto | null;
  onOpenChange: (open: boolean) => void;
}) {
  // The dialog's PickStep is skipped when prefillUser is provided; we
  // synthesize a minimal record sufficient for the ConfigureStep render.
  const prefillUser: UserDto | undefined = grant
    ? {
        id: grant.impersonatedUserId,
        userName: grant.impersonatedUserName ?? undefined,
        firstName: null,
        lastName: null,
        email: null,
        isActive: true,
        emailConfirmed: true,
      }
    : undefined;

  return (
    <ImpersonateDialog
      open={grant !== null}
      onOpenChange={onOpenChange}
      tenantId={grant?.impersonatedTenantId ?? ""}
      tenantName={grant?.impersonatedTenantId}
      prefillUser={prefillUser}
    />
  );
}

function GrantRow({
  grant,
  canRevoke,
  canReopen,
  onRevoke,
  onReopen,
}: {
  grant: ImpersonationGrantDto;
  canRevoke: boolean;
  canReopen: boolean;
  onRevoke: () => void;
  onReopen: () => void;
}) {
  const [open, setOpen] = useState(false);
  return (
    <li>
      <div className="grid grid-cols-[auto_1fr_auto_auto] items-center gap-4 px-1 py-3.5">
        <StatusGlyph status={grant.status} />
        <div className="min-w-0">
          <div className="flex flex-wrap items-baseline gap-2">
            <span className="truncate text-sm">
              <span className="font-medium">
                {grant.actorUserName ?? grant.actorUserId}
              </span>
              <span className="mx-1.5 text-[var(--color-muted-foreground)]">→</span>
              <span className="font-medium">
                {grant.impersonatedUserName ?? grant.impersonatedUserId}
              </span>
            </span>
            <Badge variant="muted" className="font-mono uppercase tracking-[0.14em]">
              {grant.impersonatedTenantId}
            </Badge>
          </div>
          <div className="mt-0.5 flex flex-wrap items-baseline gap-x-3 gap-y-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
            <span>
              <Clock className="-mt-0.5 mr-1 inline h-3 w-3" aria-hidden />
              {formatTimestamp(grant.startedAtUtc)}
            </span>
            <span>· expires {formatTimestamp(grant.expiresAtUtc)}</span>
            <button
              type="button"
              onClick={() => setOpen((v) => !v)}
              aria-expanded={open}
              aria-label={open ? "Hide grant details" : "Show grant details"}
              className="ml-1 inline-flex items-center gap-0.5 underline-offset-2 hover:text-[var(--color-foreground)] hover:underline"
            >
              {open ? "hide" : "details"}
              <ChevronDown className={cn("h-3 w-3 transition-transform", open && "rotate-180")} />
            </button>
          </div>
        </div>
        <StatusBadge status={grant.status} />
        <div className="flex shrink-0 items-center gap-2">
          {canReopen && (
            <Button
              variant="outline"
              size="sm"
              onClick={onReopen}
              title="Issue a fresh impersonation token for this user — use when you've lost the original browser tab."
            >
              <UserCog className="mr-1 h-3.5 w-3.5" /> Re-open
            </Button>
          )}
          {canRevoke ? (
            <Button variant="outline" size="sm" onClick={onRevoke}>
              <ShieldOff className="mr-1 h-3.5 w-3.5" /> Revoke
            </Button>
          ) : (
            !canReopen && <span aria-hidden />
          )}
        </div>
      </div>
      {open && <Details grant={grant} />}
    </li>
  );
}

function Details({ grant }: { grant: ImpersonationGrantDto }) {
  return (
    <dl className="ml-7 grid grid-cols-1 gap-y-1 border-l-2 border-[var(--color-accent-signal)] py-2 pl-4 text-[12px] sm:grid-cols-2 sm:gap-x-6">
      <DRow label="Reason">
        <span className="text-[var(--color-muted-foreground)]">{grant.reason || "—"}</span>
      </DRow>
      <DRow label="Grant id"><code className="code-chip">{grant.id}</code></DRow>
      <DRow label="JWT id"><code className="code-chip">{grant.jti}</code></DRow>
      <DRow label="Actor"><code className="code-chip">{grant.actorUserId}</code> @ {grant.actorTenantId}</DRow>
      <DRow label="Impersonated"><code className="code-chip">{grant.impersonatedUserId}</code></DRow>
      {grant.endedAtUtc && (
        <DRow label="Ended at">{new Date(grant.endedAtUtc).toLocaleString()}</DRow>
      )}
      {grant.revokedAtUtc && (
        <>
          <DRow label="Revoked at">{new Date(grant.revokedAtUtc).toLocaleString()}</DRow>
          <DRow label="Revoked by">{grant.revokedByUserName ?? grant.revokedByUserId ?? "—"}</DRow>
          <DRow label="Revoke reason" wide>
            <span className="text-[var(--color-muted-foreground)]">{grant.revokeReason || "—"}</span>
          </DRow>
        </>
      )}
    </dl>
  );
}

function DRow({ label, children, wide }: { label: string; children: React.ReactNode; wide?: boolean }) {
  return (
    <div className={cn("flex items-baseline gap-2", wide && "sm:col-span-2")}>
      <dt className="font-mono text-[10.5px] uppercase tracking-[0.14em] w-32 shrink-0 text-[var(--color-muted-foreground)]">{label}</dt>
      <dd className="min-w-0 truncate">{children}</dd>
    </div>
  );
}

function StatusGlyph({ status }: { status: ImpersonationGrantStatus }) {
  const tone =
    status === "Active"
      ? "bg-[var(--color-accent-signal)]"
      : status === "Revoked"
        ? "bg-[var(--color-destructive)]"
        : status === "Ended"
          ? "bg-[var(--color-info)]"
          : "bg-[var(--color-muted-foreground)]/50";
  return <span aria-hidden className={cn("h-2 w-2 rounded-full", tone)} />;
}

function StatusBadge({ status }: { status: ImpersonationGrantStatus }) {
  const variant =
    status === "Active" ? "brand" :
    status === "Revoked" ? "danger" :
    status === "Ended" ? "info" :
    "muted";
  return (
    <Badge variant={variant} className="font-mono uppercase tracking-[0.14em]">
      {status}
    </Badge>
  );
}

function formatTimestamp(value: string): string {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ShieldOff, UserCog } from "lucide-react";
import {
  listImpersonationGrants,
  type ImpersonationGrantDto,
} from "@/api/impersonation-grants";
import type { UserDto } from "@/api/users";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { SettingsSection } from "@/components/list";
import { ImpersonateDialog } from "@/components/impersonation/impersonate-dialog";
import { RevokeGrantDialog } from "@/components/impersonation/revoke-grant-dialog";
import { IdentityPermissions } from "@/lib/permissions";

const REFRESH_INTERVAL_MS = 5_000;

/**
 * ActiveGrantsCard — tenant-detail inline view of active impersonation
 * sessions targeting users in this tenant. Polls every 5s. Renders nothing
 * if the caller can't see impersonation grants (perm-gated upstream).
 */
export function ActiveGrantsCard({ tenantId }: { tenantId: string }) {
  const { user } = useAuth();
  const canView = (user?.permissions ?? []).includes(IdentityPermissions.Impersonation.View);
  const canRevoke = (user?.permissions ?? []).includes(IdentityPermissions.Impersonation.Revoke);
  const canImpersonate = (user?.permissions ?? []).includes(IdentityPermissions.Users.Impersonate);
  const currentUserId = user?.id ?? null;

  const query = useQuery({
    queryKey: ["impersonation-grants", "tenant-active", tenantId],
    queryFn: () =>
      listImpersonationGrants({
        status: "Active",
        impersonatedTenantId: tenantId,
        take: 50,
      }),
    enabled: canView,
    refetchInterval: REFRESH_INTERVAL_MS,
  });

  const [targetGrant, setTargetGrant] = useState<ImpersonationGrantDto | null>(null);
  const [reopenGrant, setReopenGrant] = useState<ImpersonationGrantDto | null>(null);

  // Minimal UserDto sufficient for ImpersonateDialog's ConfigureStep render.
  const reopenPrefillUser: UserDto | undefined = reopenGrant
    ? {
        id: reopenGrant.impersonatedUserId,
        userName: reopenGrant.impersonatedUserName ?? undefined,
        firstName: null,
        lastName: null,
        email: null,
        isActive: true,
        emailConfirmed: true,
      }
    : undefined;

  // Quiet hide when there's nothing to show — busy operators don't need
  // an empty box on every tenant page.
  if (!canView) return null;
  if (query.isLoading) return null;
  const items = query.data ?? [];
  if (items.length === 0) return null;

  return (
    <>
      <SettingsSection
        title="Active impersonations"
        icon={UserCog}
        description="Operators currently signed in as users in this tenant. Revoking immediately invalidates the issued token; the dashboard tab will 401 on its next request."
      >
        <ul className="divide-y divide-[var(--color-border)]">
          {items.map((g) => (
            <GrantRow
              key={g.id}
              grant={g}
              canRevoke={canRevoke}
              canReopen={
                canImpersonate &&
                currentUserId !== null &&
                g.actorUserId === currentUserId
              }
              onRevoke={() => setTargetGrant(g)}
              onReopen={() => setReopenGrant(g)}
            />
          ))}
        </ul>
      </SettingsSection>

      <RevokeGrantDialog
        grant={targetGrant}
        onOpenChange={(open) => !open && setTargetGrant(null)}
      />

      <ImpersonateDialog
        open={reopenGrant !== null}
        onOpenChange={(open) => !open && setReopenGrant(null)}
        tenantId={reopenGrant?.impersonatedTenantId ?? ""}
        tenantName={reopenGrant?.impersonatedTenantId}
        prefillUser={reopenPrefillUser}
      />
    </>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// GrantRow — a single active impersonation session row
// ─────────────────────────────────────────────────────────────────────────

function GrantRow({
  grant: g,
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
  return (
    <li className="grid grid-cols-[auto_1fr_auto] items-center gap-4 py-3 first:pt-0 last:pb-0">
      {/* Live-session pulse dot */}
      <span
        aria-hidden
        className="pulse-dot"
        title="Active session"
      />

      {/* Session detail */}
      <div className="min-w-0">
        <div className="flex flex-wrap items-baseline gap-2">
          <span className="text-[13px]">
            <span className="font-medium text-[var(--color-foreground)]">
              {g.actorUserName ?? g.actorUserId}
            </span>
            <span className="mx-1.5 text-[var(--color-muted-foreground)]">→</span>
            <span className="font-medium text-[var(--color-foreground)]">
              {g.impersonatedUserName ?? g.impersonatedUserId}
            </span>
          </span>
          <Badge variant="brand" className="font-mono uppercase tracking-[0.14em]">
            Active
          </Badge>
        </div>
        <div className="mt-0.5 truncate font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
          started {new Date(g.startedAtUtc).toLocaleTimeString()} · expires{" "}
          {new Date(g.expiresAtUtc).toLocaleTimeString()}
          {g.reason && <> · {truncate(g.reason, 80)}</>}
        </div>
      </div>

      {/* Row actions */}
      <RowActions
        canRevoke={canRevoke}
        // Re-open: operator's own grant + still Active + has start perm.
        // Closed-browser recovery path — issues a fresh token, leaves
        // the original grant alive until natural expiry or revoke.
        canReopen={canReopen}
        onRevoke={onRevoke}
        onReopen={onReopen}
      />
    </li>
  );
}

function RowActions({
  canRevoke,
  canReopen,
  onRevoke,
  onReopen,
}: {
  canRevoke: boolean;
  canReopen: boolean;
  onRevoke: () => void;
  onReopen: () => void;
}) {
  if (!canRevoke && !canReopen) {
    return (
      <Badge variant="muted" className="font-mono uppercase tracking-[0.14em]">
        <UserCog className="h-3 w-3" /> view-only
      </Badge>
    );
  }
  return (
    <div className="flex shrink-0 items-center gap-2">
      {canReopen && (
        <Button
          variant="outline"
          size="sm"
          onClick={onReopen}
          title="Issue a fresh impersonation token — use when you lost the original dashboard tab."
        >
          <UserCog className="mr-1 h-3.5 w-3.5" /> Re-open
        </Button>
      )}
      {canRevoke && (
        <Button variant="outline" size="sm" onClick={onRevoke}>
          <ShieldOff className="mr-1 h-3.5 w-3.5" /> Revoke
        </Button>
      )}
    </div>
  );
}

function truncate(s: string, n: number): string {
  return s.length > n ? `${s.slice(0, n - 1)}…` : s;
}

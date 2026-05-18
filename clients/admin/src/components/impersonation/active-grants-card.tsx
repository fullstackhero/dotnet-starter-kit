import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ShieldOff, UserCog } from "lucide-react";
import {
  listImpersonationGrants,
  type ImpersonationGrantDto,
} from "@/api/impersonation-grants";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { FormSection, FormShell } from "@/components/list";
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

  // Quiet hide when there's nothing to show — busy operators don't need
  // an empty box on every tenant page.
  if (!canView) return null;
  if (query.isLoading) return null;
  const items = query.data ?? [];
  if (items.length === 0) return null;

  return (
    <>
      <FormShell>
        <FormSection
          title="Active impersonations"
          description="Operators currently signed in as users in this tenant. Revoking immediately invalidates the issued token; the dashboard tab will 401 on its next request."
        >
          <ul className="divide-y divide-[var(--color-border)] border-y border-[var(--color-border)]">
            {items.map((g) => (
              <li
                key={g.id}
                className="grid grid-cols-[auto_1fr_auto] items-center gap-4 py-3"
              >
                <span
                  aria-hidden
                  className="pulse-dot"
                  title="Active session"
                />
                <div className="min-w-0">
                  <div className="flex flex-wrap items-baseline gap-2">
                    <span className="text-sm">
                      <span className="font-medium">
                        {g.actorUserName ?? g.actorUserId}
                      </span>
                      <span className="mx-1.5 text-[var(--color-muted-foreground)]">→</span>
                      <span className="font-medium">
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
                {canRevoke ? (
                  <Button variant="outline" size="sm" onClick={() => setTargetGrant(g)}>
                    <ShieldOff className="mr-1 h-3.5 w-3.5" /> Revoke
                  </Button>
                ) : (
                  <Badge variant="muted" className="font-mono uppercase tracking-[0.14em]">
                    <UserCog className="h-3 w-3" /> view-only
                  </Badge>
                )}
              </li>
            ))}
          </ul>
        </FormSection>
      </FormShell>

      <RevokeGrantDialog
        grant={targetGrant}
        onOpenChange={(open) => !open && setTargetGrant(null)}
      />
    </>
  );
}

function truncate(s: string, n: number): string {
  return s.length > n ? `${s.slice(0, n - 1)}…` : s;
}

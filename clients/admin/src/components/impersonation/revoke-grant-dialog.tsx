import { useEffect, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ShieldOff } from "lucide-react";
import { toast } from "sonner";
import {
  revokeImpersonationGrant,
  type ImpersonationGrantDto,
} from "@/api/impersonation-grants";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

type Props = {
  grant: ImpersonationGrantDto | null;
  onOpenChange: (open: boolean) => void;
  /** Optional callback after a successful revoke — receives the updated grant. */
  onRevoked?: (updated: ImpersonationGrantDto) => void;
};

/**
 * RevokeGrantDialog — confirmation modal for revoking an active impersonation
 * grant. Asks for an optional reason (recorded on the grant + in the security
 * audit trail) and surfaces the impersonated user / actor / tenant trio so the
 * operator knows exactly what they're killing.
 */
export function RevokeGrantDialog({ grant, onOpenChange, onRevoked }: Props) {
  const queryClient = useQueryClient();
  const [reason, setReason] = useState("");
  const open = grant !== null;

  // Reset reason whenever a new grant is targeted (or the dialog closes).
  useEffect(() => {
    if (open) setReason("");
  }, [open, grant?.id]);

  const mutation = useMutation<ImpersonationGrantDto, Error, void>({
    mutationFn: () => revokeImpersonationGrant(grant!.id, reason.trim() || undefined),
    onSuccess: (updated) => {
      toast.success("Impersonation revoked", {
        description: `Token for ${updated.impersonatedUserName ?? updated.impersonatedUserId} will be rejected on the next request.`,
      });
      queryClient.invalidateQueries({ queryKey: ["impersonation-grants"] });
      onRevoked?.(updated);
      onOpenChange(false);
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : err.message;
      toast.error("Revoke failed", { description: detail });
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent size="md">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <span
              aria-hidden
              className="grid h-7 w-7 place-items-center rounded-md bg-[var(--color-destructive)]/15 text-[var(--color-destructive)]"
            >
              <ShieldOff className="h-4 w-4" />
            </span>
            <DialogTitle>Revoke impersonation grant</DialogTitle>
          </div>
          <DialogDescription>
            The issued token will be rejected on the next authenticated request (within ~1
            second of cache TTL). The session in the dashboard tab is killed without warning.
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="space-y-4">
          {grant && <GrantSummary grant={grant} />}

          <div className="space-y-1.5">
            <label
              htmlFor="revoke-reason"
              className="meta text-[var(--color-muted-foreground)]"
            >
              Reason (optional)
            </label>
            <textarea
              id="revoke-reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="e.g. Operator left for lunch; ending session"
              rows={3}
              maxLength={500}
              className={cn(
                "w-full resize-y rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm",
                "transition-[border-color,background-color,box-shadow] duration-[var(--duration-fast)]",
                "hover:border-[var(--color-border-strong)]",
                "focus-visible:outline-none focus-visible:border-[var(--color-accent-signal)] focus-visible:ring-2 focus-visible:ring-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.25)] focus-visible:bg-[var(--color-surface-2)]",
              )}
            />
            <p className="text-[11px] text-[var(--color-muted-foreground)]">
              Recorded on the grant and in the security audit trail.
            </p>
          </div>
        </DialogBody>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={mutation.isPending}>
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending}
          >
            <ShieldOff className="mr-1 h-3.5 w-3.5" />
            {mutation.isPending ? "Revoking…" : "Revoke now"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function GrantSummary({ grant }: { grant: ImpersonationGrantDto }) {
  return (
    <div className="space-y-2 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-3 py-3">
      <Row label="Impersonating">
        <span className="font-medium">
          {grant.impersonatedUserName ?? grant.impersonatedUserId}
        </span>{" "}
        <Badge variant="muted" className="ml-1 font-mono uppercase tracking-[0.14em]">
          {grant.impersonatedTenantId}
        </Badge>
      </Row>
      <Row label="Started by">
        <span>{grant.actorUserName ?? grant.actorUserId}</span>{" "}
        <Badge variant="muted" className="ml-1 font-mono uppercase tracking-[0.14em]">
          {grant.actorTenantId}
        </Badge>
      </Row>
      <Row label="Reason">
        <span className="text-[var(--color-muted-foreground)]">{grant.reason || "—"}</span>
      </Row>
      <Row label="Expires">
        <code className="code-chip">{new Date(grant.expiresAtUtc).toLocaleString()}</code>
      </Row>
    </div>
  );
}

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="grid grid-cols-[7rem_1fr] items-baseline gap-3">
      <span className="meta text-[var(--color-muted-foreground)]">{label}</span>
      <span className="min-w-0 text-sm">{children}</span>
    </div>
  );
}

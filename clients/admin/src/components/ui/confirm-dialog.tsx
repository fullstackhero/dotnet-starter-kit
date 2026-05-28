import type { ReactNode } from "react";
import { AlertTriangle } from "lucide-react";
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
import { cn } from "@/lib/cn";

/**
 * A reusable confirmation dialog for important / irreversible actions. Replaces ad-hoc
 * window.confirm calls with a styled, accessible Radix dialog. The confirm button shows a pending
 * state while the action runs and the dialog stays open until the caller closes it.
 */
export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  onConfirm,
  destructive = false,
  pending = false,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void;
  destructive?: boolean;
  pending?: boolean;
}) {
  return (
    <Dialog open={open} onOpenChange={(o) => (pending ? undefined : onOpenChange(o))}>
      <DialogContent size="sm">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className={cn(
                "grid h-9 w-9 shrink-0 place-items-center rounded-xl ring-1 ring-inset",
                destructive
                  ? "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.12)] text-[var(--color-destructive)] ring-[oklch(from_var(--color-destructive)_l_c_h_/_0.18)]"
                  : "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.12)] text-[var(--color-primary)] ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]",
              )}
            >
              <AlertTriangle className="h-[18px] w-[18px]" />
            </span>
            <DialogTitle className="text-[16px]">{title}</DialogTitle>
          </div>
        </DialogHeader>

        <DialogBody>
          <DialogDescription>{description}</DialogDescription>
        </DialogBody>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
            {cancelLabel}
          </Button>
          <Button
            type="button"
            variant={destructive ? "destructive" : "default"}
            onClick={onConfirm}
            disabled={pending}
          >
            {pending ? "Working…" : confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

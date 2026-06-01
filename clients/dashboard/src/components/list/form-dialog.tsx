import * as React from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

/**
 * FormDialog — shell for the repeated editor-dialog pattern used across the
 * dashboard (brands, categories, products, ticket create…): a Radix Dialog
 * wrapping a <form> with a header (title + description), a scrollable body,
 * and a footer carrying a Cancel button + a primary Submit button that
 * disables and swaps to a pending label while a mutation is in flight.
 *
 * It does NOT own form state — callers keep their useState/useMutation and
 * pass `onSubmit`. This is purely the chrome so editor dialogs stop
 * hand-rolling the same Header/Body/Footer + Cancel/Submit scaffolding.
 *
 * Example:
 *   <FormDialog
 *     open={isOpen}
 *     onClose={onClose}
 *     title={editing ? "Edit brand" : "Add a brand"}
 *     description="…"
 *     onSubmit={handleSubmit}
 *     pending={isPending}
 *     submitLabel={editing ? "Save changes" : "Add brand"}
 *     submitDisabled={!trimmedName}
 *   >
 *     <Field …>…</Field>
 *   </FormDialog>
 */
export function FormDialog({
  open,
  onClose,
  title,
  description,
  onSubmit,
  pending = false,
  submitLabel,
  pendingLabel = "Saving…",
  cancelLabel = "Cancel",
  submitDisabled = false,
  submitVariant = "default",
  contentClassName,
  bodyClassName,
  children,
  footerExtra,
}: {
  open: boolean;
  /** Fired when the dialog requests close (overlay click, Esc, Cancel, ✕). */
  onClose: () => void;
  title: React.ReactNode;
  description?: React.ReactNode;
  /** Submit handler — receives the form event with preventDefault already wired. */
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
  /** Mutation in flight: disables both buttons and shows `pendingLabel`. */
  pending?: boolean;
  submitLabel: React.ReactNode;
  pendingLabel?: React.ReactNode;
  cancelLabel?: React.ReactNode;
  /** Extra gate on the submit button (e.g. empty required field). */
  submitDisabled?: boolean;
  submitVariant?: React.ComponentProps<typeof Button>["variant"];
  /** Override DialogContent sizing (e.g. "!max-w-lg"). */
  contentClassName?: string;
  bodyClassName?: string;
  children: React.ReactNode;
  /** Optional node rendered at the left of the footer (e.g. a hint). */
  footerExtra?: React.ReactNode;
}) {
  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className={contentClassName}>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            onSubmit(e);
          }}
        >
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
            {description && <DialogDescription>{description}</DialogDescription>}
          </DialogHeader>

          <DialogBody className={bodyClassName}>{children}</DialogBody>

          <DialogFooter>
            {footerExtra && (
              <div className="mr-auto flex items-center text-[11.5px] text-[var(--color-muted-foreground)]">
                {footerExtra}
              </div>
            )}
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={pending}>
                {cancelLabel}
              </Button>
            </DialogClose>
            <Button
              type="submit"
              variant={submitVariant}
              disabled={pending || submitDisabled}
            >
              {pending ? pendingLabel : submitLabel}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

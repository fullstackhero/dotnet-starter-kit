import * as React from "react";
import * as DialogPrimitive from "@radix-ui/react-dialog";
import { X } from "lucide-react";
import { cn } from "@/lib/cn";

/**
 * Dialog primitives — Radix-based, styled to the FSH design system.
 * Usage:
 *   <Dialog open={open} onOpenChange={setOpen}>
 *     <DialogContent>
 *       <DialogHeader>
 *         <DialogTitle>...</DialogTitle>
 *         <DialogDescription>...</DialogDescription>
 *       </DialogHeader>
 *       <DialogFooter>...</DialogFooter>
 *     </DialogContent>
 *   </Dialog>
 *
 * Open/close transitions are driven by [data-state] attributes Radix
 * sets on the overlay and content, paired with the keyframes below.
 */

export const Dialog = DialogPrimitive.Root;
export const DialogTrigger = DialogPrimitive.Trigger;
export const DialogPortal = DialogPrimitive.Portal;
export const DialogClose = DialogPrimitive.Close;

export const DialogOverlay = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Overlay>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Overlay>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Overlay
    ref={ref}
    className={cn(
      "fixed inset-0 z-50 bg-[oklch(from_var(--color-background)_l_c_h_/_0.55)] backdrop-blur-md",
      "data-[state=open]:animate-fsh-overlay-in data-[state=closed]:animate-fsh-overlay-out",
      className,
    )}
    {...props}
  />
));
DialogOverlay.displayName = "DialogOverlay";

export const DialogContent = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Content>
>(({ className, children, ...props }, ref) => (
  <DialogPortal>
    <DialogOverlay />
    <DialogPrimitive.Content
      ref={ref}
      className={cn(
        "fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2",
        // Architectural surface — single hairline border, no gradient ramp.
        // The lift shadow is intentionally larger than card-shell hover
        // (this is a modal — it should clearly float above the page).
        "card-shell rounded-2xl shadow-[var(--shadow-lift)]",
        "data-[state=open]:animate-fsh-dialog-in data-[state=closed]:animate-fsh-dialog-out",
        className,
      )}
      {...props}
    >
      {children}
      <DialogPrimitive.Close
        aria-label="Close"
        className={cn(
          "absolute right-3 top-3 grid h-7 w-7 place-items-center rounded-md",
          "text-[var(--color-muted-foreground)] transition-colors",
          "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        )}
      >
        <X className="h-4 w-4" />
      </DialogPrimitive.Close>
    </DialogPrimitive.Content>
  </DialogPortal>
));
DialogContent.displayName = "DialogContent";

export function DialogHeader({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("flex flex-col gap-1.5 px-6 pb-3 pt-6 text-left", className)}
      {...props}
    />
  );
}

export function DialogFooter({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "flex flex-col-reverse gap-2 border-t border-[var(--color-border)] px-6 py-4",
        "sm:flex-row sm:justify-end",
        className,
      )}
      {...props}
    />
  );
}

export const DialogTitle = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Title>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Title>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Title
    ref={ref}
    className={cn("text-display text-lg font-semibold leading-tight tracking-tight", className)}
    {...props}
  />
));
DialogTitle.displayName = "DialogTitle";

export const DialogDescription = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Description>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Description>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Description
    ref={ref}
    className={cn("text-sm leading-relaxed text-[var(--color-muted-foreground)]", className)}
    {...props}
  />
));
DialogDescription.displayName = "DialogDescription";

export function DialogBody({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("px-6 pb-5 pt-1", className)} {...props} />;
}

// ─────────────────────────────────────────────────────────────────────────
// Sheet — edge-anchored Dialog variant. Same Radix primitive (so it gets
// focus-trap, scroll-lock, ESC dismissal, overlay click-to-close for free)
// but slides in from a side instead of centering. Used for the mobile
// nav drawer; available for any other surface that wants edge-anchored
// affordances (filter panels, detail drawers, etc.).
// ─────────────────────────────────────────────────────────────────────────

type SheetSide = "left" | "right" | "top" | "bottom";

const sheetSideClasses: Record<SheetSide, string> = {
  left: "inset-y-0 left-0 h-full w-[min(20rem,85vw)] border-r data-[state=open]:animate-fsh-sheet-in-left data-[state=closed]:animate-fsh-sheet-out-left",
  right: "inset-y-0 right-0 h-full w-[min(20rem,85vw)] border-l data-[state=open]:animate-fsh-sheet-in-right data-[state=closed]:animate-fsh-sheet-out-right",
  top: "inset-x-0 top-0 w-full max-h-[85vh] border-b data-[state=open]:animate-fsh-sheet-in-top data-[state=closed]:animate-fsh-sheet-out-top",
  bottom: "inset-x-0 bottom-0 w-full max-h-[85vh] border-t data-[state=open]:animate-fsh-sheet-in-bottom data-[state=closed]:animate-fsh-sheet-out-bottom",
};

export const SheetContent = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Content> & {
    side?: SheetSide;
    /** Render the built-in close button. Defaults to true. */
    showClose?: boolean;
  }
>(({ className, children, side = "right", showClose = true, ...props }, ref) => (
  <DialogPortal>
    <DialogOverlay />
    <DialogPrimitive.Content
      ref={ref}
      className={cn(
        "fixed z-50 flex flex-col bg-[var(--color-surface-1)] border-[var(--color-border)] shadow-[var(--shadow-lift)]",
        sheetSideClasses[side],
        className,
      )}
      {...props}
    >
      {children}
      {showClose && (
        <DialogPrimitive.Close
          aria-label="Close"
          className={cn(
            "absolute right-3 top-3 grid h-7 w-7 place-items-center rounded-md",
            "text-[var(--color-muted-foreground)] transition-colors",
            "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          )}
        >
          <X className="h-4 w-4" />
        </DialogPrimitive.Close>
      )}
    </DialogPrimitive.Content>
  </DialogPortal>
));
SheetContent.displayName = "SheetContent";

// Aliases — when authors prefer reading <Sheet>/<SheetTrigger>/<SheetClose>
// over <Dialog>/<DialogTrigger>/<DialogClose> for sheet usage. The runtime
// is identical; this is purely about call-site clarity.
export const Sheet = DialogPrimitive.Root;
export const SheetTrigger = DialogPrimitive.Trigger;
export const SheetClose = DialogPrimitive.Close;

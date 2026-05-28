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
 *       <DialogBody>...</DialogBody>
 *       <DialogFooter>...</DialogFooter>
 *     </DialogContent>
 *   </Dialog>
 *
 * Open/close transitions are driven by [data-state] attributes Radix
 * sets on the overlay and content, paired with the keyframes in
 * globals.css.
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
    data-slot="dialog-overlay"
    className={cn(
      "fixed inset-0 z-50 bg-[oklch(0_0_0_/_0.4)] backdrop-blur-[6px]",
      "data-[state=open]:animate-fsh-overlay-in data-[state=closed]:animate-fsh-overlay-out",
      className,
    )}
    {...props}
  />
));
DialogOverlay.displayName = "DialogOverlay";

export const DialogContent = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Content> & {
    /**
     * Max width — defaults to `md`. Preserved for admin call-site compat.
     * `sm` = max-w-sm, `md` = max-w-lg (≈ dashboard default), `lg` = max-w-2xl, `xl` = max-w-4xl.
     */
    size?: "sm" | "md" | "lg" | "xl";
  }
>(({ className, children, size = "md", ...props }, ref) => {
  const sizeClass: Record<NonNullable<typeof size>, string> = {
    sm: "sm:max-w-sm",
    md: "sm:max-w-lg",
    lg: "sm:max-w-2xl",
    xl: "sm:max-w-4xl",
  };
  return (
    <DialogPortal>
      <DialogOverlay />
      <DialogPrimitive.Content
        ref={ref}
        data-slot="dialog-content"
        className={cn(
          "fixed left-1/2 top-1/2 z-50 grid w-full max-w-[calc(100%-2rem)] -translate-x-1/2 -translate-y-1/2",
          // Cap height to the viewport and scroll when content overflows so the footer
          // (submit/cancel) stays reachable on short viewports and tall forms.
          "max-h-[calc(100dvh-2rem)] overflow-y-auto",
          sizeClass[size],
          "rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]",
          "shadow-xl outline-none",
          "data-[state=open]:animate-fsh-dialog-in data-[state=closed]:animate-fsh-dialog-out",
          className,
        )}
        {...props}
      >
        {children}
        <DialogPrimitive.Close
          data-slot="dialog-close"
          aria-label="Close"
          className={cn(
            "absolute top-3.5 right-3.5 size-9 rounded-lg flex items-center justify-center",
            "text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)] hover:text-[var(--color-foreground)]",
            "hover:bg-[var(--color-accent)] transition-colors cursor-pointer outline-none",
            "focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
            "[&_svg]:pointer-events-none [&_svg]:shrink-0",
          )}
        >
          <X className="size-4" />
        </DialogPrimitive.Close>
      </DialogPrimitive.Content>
    </DialogPortal>
  );
});
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
    className={cn("font-display text-lg font-semibold leading-tight tracking-tight", className)}
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
// but slides in from a side instead of centering.
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

// Aliases for call-site clarity when using sheet semantics.
export const Sheet = DialogPrimitive.Root;
export const SheetTrigger = DialogPrimitive.Trigger;
export const SheetClose = DialogPrimitive.Close;

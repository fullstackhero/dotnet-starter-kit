import * as React from "react";
import * as DialogPrimitive from "@radix-ui/react-dialog";
import { X } from "lucide-react";
import { cn } from "@/lib/cn";

/**
 * Dialog — Console-flavored Radix Dialog.
 * Pattern:
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
      "transition-opacity duration-200 data-[state=open]:opacity-100 data-[state=closed]:opacity-0",
      className,
    )}
    {...props}
  />
));
DialogOverlay.displayName = "DialogOverlay";

export const DialogContent = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Content> & {
    /** Max width — defaults to `md` (28rem). Use `lg`/`xl` for richer dialogs. */
    size?: "sm" | "md" | "lg" | "xl";
  }
>(({ className, children, size = "md", ...props }, ref) => {
  const sizeClass: Record<NonNullable<typeof size>, string> = {
    sm: "max-w-sm",
    md: "max-w-md",
    lg: "max-w-2xl",
    xl: "max-w-4xl",
  };
  return (
    <DialogPortal>
      <DialogOverlay />
      <DialogPrimitive.Content
        ref={ref}
        className={cn(
          "fixed left-1/2 top-1/2 z-50 w-[calc(100vw-2rem)] -translate-x-1/2 -translate-y-1/2",
          sizeClass[size],
          "card-shell rounded-xl shadow-[0_24px_64px_-24px_oklch(0_0_0/0.30),0_8px_24px_-8px_oklch(0_0_0/0.18)]",
          "transition-[opacity,transform] duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
          "data-[state=open]:opacity-100 data-[state=closed]:opacity-0",
          "data-[state=open]:scale-100 data-[state=closed]:scale-[0.97]",
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
  );
});
DialogContent.displayName = "DialogContent";

export function DialogHeader({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div className={cn("flex flex-col gap-1.5 px-6 pb-3 pt-6 text-left", className)} {...props} />
  );
}

export function DialogBody({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("px-6 pb-5 pt-1", className)} {...props} />;
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

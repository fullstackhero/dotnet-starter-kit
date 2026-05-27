import * as React from "react";
import * as DropdownMenuPrimitive from "@radix-ui/react-dropdown-menu";
import { ChevronRight } from "lucide-react";
import { cn } from "@/lib/cn";

/**
 * Dropdown primitives — Radix-based, styled to the FSH design system.
 * Trigger transitions are driven by [data-state] attributes Radix sets
 * on the content, paired with the dialog-in/out keyframes from
 * globals.css. Content uses the gradient-border + frosted treatment so
 * it shares vocabulary with Card and Dialog.
 */

export const DropdownMenu = DropdownMenuPrimitive.Root;
export const DropdownMenuTrigger = DropdownMenuPrimitive.Trigger;
export const DropdownMenuPortal = DropdownMenuPrimitive.Portal;
export const DropdownMenuGroup = DropdownMenuPrimitive.Group;

export const DropdownMenuContent = React.forwardRef<
  React.ComponentRef<typeof DropdownMenuPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof DropdownMenuPrimitive.Content>
>(({ className, sideOffset = 6, ...props }, ref) => (
  <DropdownMenuPortal>
    <DropdownMenuPrimitive.Content
      ref={ref}
      data-slot="dropdown-menu-content"
      sideOffset={sideOffset}
      className={cn(
        "z-50 min-w-[12rem] max-h-[var(--radix-dropdown-menu-content-available-height)]",
        "overflow-x-hidden overflow-y-auto",
        "rounded-lg border border-[var(--color-border)] bg-[var(--color-popover)] p-1",
        "text-[var(--color-popover-foreground)] shadow-md",
        "origin-[var(--radix-dropdown-menu-content-transform-origin)]",
        "data-[state=open]:animate-fsh-dialog-in data-[state=closed]:animate-fsh-dialog-out",
        className,
      )}
      {...props}
    />
  </DropdownMenuPortal>
));
DropdownMenuContent.displayName = "DropdownMenuContent";

export const DropdownMenuItem = React.forwardRef<
  React.ComponentRef<typeof DropdownMenuPrimitive.Item>,
  React.ComponentPropsWithoutRef<typeof DropdownMenuPrimitive.Item> & {
    destructive?: boolean;
  }
>(({ className, destructive, ...props }, ref) => (
  <DropdownMenuPrimitive.Item
    ref={ref}
    className={cn(
      // The `group` class lets descendants react to [data-highlighted]
      // via Tailwind's group-data-* selector (see QuickAction in topbar).
      "group relative mx-1 my-0.5 flex cursor-pointer select-none items-center gap-2.5 rounded-md px-2.5 py-2 text-sm",
      "data-[disabled]:cursor-not-allowed",
      // Radix moves keyboard focus to the highlighted item, which would
      // otherwise trigger the global :focus-visible halo on top of the
      // bg-accent hover signal — visible as a thick brand outline.
      // Suppress the focus chrome here; data-[highlighted] is the
      // single source of truth for "this row is active".
      "outline-none focus:outline-none focus-visible:outline-none focus-visible:shadow-none",
      "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
      "data-[highlighted]:bg-[var(--color-accent)]",
      destructive
        ? "text-[var(--color-destructive)] data-[highlighted]:bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] data-[highlighted]:text-[var(--color-destructive)]"
        : "text-[var(--color-foreground)] data-[highlighted]:text-[var(--color-foreground)]",
      "data-[disabled]:pointer-events-none data-[disabled]:opacity-50",
      className,
    )}
    {...props}
  />
));
DropdownMenuItem.displayName = "DropdownMenuItem";

export const DropdownMenuLinkItem = React.forwardRef<
  React.ComponentRef<typeof DropdownMenuPrimitive.Item>,
  React.ComponentPropsWithoutRef<typeof DropdownMenuPrimitive.Item> & {
    href: string;
  }
>(({ href, children, ...props }, ref) => {
  return (
    // `asChild` makes the anchor itself the menuitem (keyboard-reachable,
    // single role) rather than nesting an <a> inside a role="menuitem".
    // Radix's Slot merges the Item's handlers (onClick/onSelect) onto the
    // anchor, so they ride through `...props`.
    <DropdownMenuItem ref={ref} asChild {...props}>
      <a href={href} className="w-full">
        {children}
        <ChevronRight className="ml-auto h-3.5 w-3.5 text-[var(--color-muted-foreground)]" aria-hidden />
      </a>
    </DropdownMenuItem>
  );
});
DropdownMenuLinkItem.displayName = "DropdownMenuLinkItem";

export const DropdownMenuLabel = React.forwardRef<
  React.ComponentRef<typeof DropdownMenuPrimitive.Label>,
  React.ComponentPropsWithoutRef<typeof DropdownMenuPrimitive.Label>
>(({ className, ...props }, ref) => (
  <DropdownMenuPrimitive.Label
    ref={ref}
    className={cn(
      "px-3 pt-3 pb-1 font-mono text-[10.5px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]",
      className,
    )}
    {...props}
  />
));
DropdownMenuLabel.displayName = "DropdownMenuLabel";

export const DropdownMenuSeparator = React.forwardRef<
  React.ComponentRef<typeof DropdownMenuPrimitive.Separator>,
  React.ComponentPropsWithoutRef<typeof DropdownMenuPrimitive.Separator>
>(({ className, ...props }, ref) => (
  <DropdownMenuPrimitive.Separator
    ref={ref}
    className={cn("my-1 h-px bg-[var(--color-border)]", className)}
    {...props}
  />
));
DropdownMenuSeparator.displayName = "DropdownMenuSeparator";

/**
 * A non-interactive row meant to host arbitrary content (e.g. a
 * segmented theme toggle). Sits on the same horizontal rail as items
 * but doesn't participate in roving focus.
 */
export function DropdownMenuRow({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("mx-1 my-0.5 flex items-center gap-2.5 rounded-md px-2.5 py-2", className)}
      {...props}
    />
  );
}

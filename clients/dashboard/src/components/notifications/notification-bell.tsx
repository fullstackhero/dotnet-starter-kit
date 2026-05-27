import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Bell, Check, MessageCircle } from "lucide-react";
import {
  getUnreadCount,
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  type NotificationDto,
} from "@/api/notifications";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { RealtimeStatusPill } from "@/components/realtime/realtime-status-pill";
import { cn } from "@/lib/cn";

/**
 * Bell icon + dropdown inbox. Calm header + scrollable list with a
 * "Mark all read" affordance. When the inbox is open and a
 * NotificationCreated event lands, we patch cache so the badge updates
 * without a refetch.
 */
export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const unreadQuery = useQuery({
    queryKey: ["notifications", "unread-count"],
    queryFn: getUnreadCount,
    staleTime: 30_000,
  });

  const inboxQuery = useQuery({
    queryKey: ["notifications", "inbox"],
    queryFn: () => listNotifications({ pageSize: 30 }),
    enabled: open,
    staleTime: 0,
  });

  // Live patch — increment badge count + prepend row when the realtime
  // event fires. We don't depend on `open` here because the badge needs
  // to keep updating regardless.
  useRealtimeEvent<NotificationDto>("NotificationCreated", (payload) => {
    queryClient.setQueryData<number | undefined>(
      ["notifications", "unread-count"],
      (prev) => (typeof prev === "number" ? prev + 1 : 1),
    );
    queryClient.setQueryData<NotificationDto[] | undefined>(
      ["notifications", "inbox"],
      (prev) => (prev ? [payload, ...prev] : [payload]),
    );
  });

  const markAllMutation = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: () => {
      queryClient.setQueryData(["notifications", "unread-count"], 0);
      void queryClient.invalidateQueries({ queryKey: ["notifications", "inbox"] });
    },
  });

  const markOneMutation = useMutation({
    mutationFn: (id: string) => markNotificationRead(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["notifications", "unread-count"] });
      void queryClient.invalidateQueries({ queryKey: ["notifications", "inbox"] });
    },
  });

  const unread = unreadQuery.data ?? 0;
  const inbox = inboxQuery.data ?? [];

  const onItemSelect = (n: NotificationDto) => {
    if (!n.readAtUtc) markOneMutation.mutate(n.id);
    if (n.link) {
      setOpen(false);
      navigate(n.link);
    }
  };

  return (
    <DropdownMenu open={open} onOpenChange={setOpen} modal={false}>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          data-notification-bell
          aria-label={`Notifications${unread > 0 ? `, ${unread} unread` : ""}`}
          title="Notifications"
          className={cn(
            "relative grid h-9 w-9 cursor-pointer place-items-center rounded-md",
            "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            "data-[state=open]:bg-[var(--color-accent)] data-[state=open]:text-[var(--color-foreground)]",
          )}
        >
          <Bell className="h-4 w-4" aria-hidden />
          {unread > 0 && (
            <span
              aria-hidden
              className={cn(
                "absolute -right-0.5 -top-0.5 grid min-w-[16px] place-items-center rounded-full",
                "h-4 px-1 text-[10px] font-semibold tabular-nums leading-none",
                "bg-[var(--color-destructive)] text-[var(--color-destructive-foreground)]",
                "ring-2 ring-[var(--color-background)]",
              )}
            >
              {unread > 99 ? "99+" : unread}
            </span>
          )}
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" sideOffset={10} className="w-[380px] p-0">
        <div className="flex items-start justify-between gap-3 border-b border-border bg-card px-4 pb-3 pt-4">
          <div className="min-w-0">
            <div className="font-display text-sm font-semibold tracking-tight">
              Notifications
            </div>
            <div className="text-[12px] text-[var(--color-muted-foreground)]">
              {unread === 0
                ? "All caught up"
                : `${unread} unread · ${inbox.length} loaded`}
            </div>
          </div>
          {unread > 0 && (
            <button
              type="button"
              onClick={() => markAllMutation.mutate()}
              disabled={markAllMutation.isPending}
              className={cn(
                "inline-flex h-7 cursor-pointer items-center gap-1 rounded-md border px-2",
                "border-border bg-card",
                "text-[11px] font-medium text-[var(--color-foreground)]",
                "hover:border-[var(--color-primary)] hover:text-[var(--color-primary)]",
                "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              )}
            >
              <Check className="h-3 w-3" aria-hidden />
              Mark all read
            </button>
          )}
        </div>

        <DropdownMenuLabel className="!my-0">Recent</DropdownMenuLabel>

        <div className="max-h-[400px] overflow-y-auto">
          {inboxQuery.isLoading ? (
            <p className="px-4 py-6 text-center text-[12px] text-[var(--color-muted-foreground)]">
              Loading…
            </p>
          ) : inbox.length === 0 ? (
            <p className="px-4 py-6 text-center text-sm text-[var(--color-muted-foreground)]">
              Nothing yet. Mentions and channel updates will appear here.
            </p>
          ) : (
            <ul className="px-1.5 pb-2">
              {inbox.map((n) => (
                <li key={n.id}>
                  <NotificationRow notification={n} onSelect={() => onItemSelect(n)} />
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="flex items-center justify-between border-t border-[var(--color-border)] px-4 py-2.5">
          <RealtimeStatusPill announce />
          <button
            type="button"
            onClick={() => {
              setOpen(false);
              navigate("/settings/notifications");
            }}
            className="text-[11px] text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
          >
            Settings ↗
          </button>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function NotificationRow({
  notification,
  onSelect,
}: {
  notification: NotificationDto;
  onSelect: () => void;
}) {
  const isUnread = notification.readAtUtc === null || notification.readAtUtc === undefined;
  const time = useMemo(() => relativeTime(notification.createdAtUtc), [notification.createdAtUtc]);
  return (
    <button
      type="button"
      onClick={onSelect}
      className={cn(
        "group/notif flex w-full cursor-pointer items-start gap-2.5 rounded-lg p-2.5 text-left",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        isUnread
          ? "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.04)] hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.08)]"
          : "hover:bg-[var(--color-accent)]",
      )}
    >
      {/* Type icon chip — currently only chat.mention; switch when more types ship. */}
      <span
        aria-hidden
        className={cn(
          "grid h-7 w-7 shrink-0 place-items-center rounded-md",
          isUnread
            ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
        )}
      >
        <MessageCircle className="h-3.5 w-3.5" />
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex items-baseline justify-between gap-2">
          <span
            className={cn(
              "truncate text-sm leading-tight",
              isUnread ? "font-semibold text-[var(--color-foreground)]" : "text-[var(--color-foreground)]",
            )}
          >
            {notification.title}
          </span>
          <span className="shrink-0 text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
            {time}
          </span>
        </div>
        {notification.body && (
          <p className="mt-0.5 line-clamp-2 text-[12.5px] text-[var(--color-muted-foreground)]">
            {notification.body}
          </p>
        )}
      </div>

      {isUnread && (
        <span
          aria-hidden
          className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-[var(--color-primary)]"
        />
      )}
    </button>
  );
}

function relativeTime(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const seconds = Math.max(0, Math.round(diffMs / 1000));
  if (seconds < 60) return "just now";
  const minutes = Math.round(seconds / 60);
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours}h`;
  const days = Math.round(hours / 24);
  if (days < 7) return `${days}d`;
  const weeks = Math.round(days / 7);
  if (weeks < 5) return `${weeks}w`;
  return new Date(iso).toLocaleDateString("en-US", { month: "short", day: "numeric" });
}

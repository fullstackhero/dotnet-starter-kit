import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Bell, BellRing, CheckCheck } from "lucide-react";
import { toast } from "sonner";
import {
  getUnreadCount,
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  type NotificationDto,
} from "@/api/notifications";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";

/**
 * NotificationBell — topbar trigger with unread badge and a popover preview
 * of the most recent items. Live-updates via the SignalR NotificationCreated
 * event (bumps the unread query + flashes the bell). Full inbox lives at
 * /notifications.
 */
export function NotificationBell() {
  const { isAuthenticated } = useAuth();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [pulse, setPulse] = useState(false);

  const unread = useQuery({
    queryKey: ["notifications", "unread-count"],
    queryFn: getUnreadCount,
    enabled: isAuthenticated,
    staleTime: 30_000,
    refetchInterval: 60_000,
  });

  const recent = useQuery({
    queryKey: ["notifications", "recent"],
    queryFn: () => listNotifications({ pageSize: 8 }),
    enabled: isAuthenticated && open,
    staleTime: 15_000,
  });

  // Coalesce a burst of NotificationCreated events into a single refetch so a
  // flood doesn't trigger a flood of badge/preview queries.
  const refreshTimer = useRef<number | null>(null);
  useRealtimeEvent<unknown>("NotificationCreated", () => {
    setPulse(true);
    window.setTimeout(() => setPulse(false), 1500);
    if (refreshTimer.current !== null) return;
    refreshTimer.current = window.setTimeout(() => {
      refreshTimer.current = null;
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    }, 800);
  });
  useEffect(
    () => () => {
      if (refreshTimer.current !== null) window.clearTimeout(refreshTimer.current);
    },
    [],
  );

  // Close the popover on Escape (it isn't a focus-trapping modal).
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [open]);

  const markOne = useMutation({
    mutationFn: (id: string) => markNotificationRead(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });

  const markAll = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: (data) => {
      toast.success(`${data.updated} ${data.updated === 1 ? "notification" : "notifications"} marked read`);
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },
  });

  if (!isAuthenticated) return null;

  const count = unread.data ?? 0;
  const items = recent.data ?? [];

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={count > 0 ? `${count} unread notifications` : "Notifications"}
        aria-haspopup="true"
        aria-expanded={open}
        className={cn(
          "relative grid h-8 w-8 place-items-center rounded-md text-[var(--color-muted-foreground)]",
          "transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        )}
      >
        {pulse ? <BellRing className="h-4 w-4 text-[var(--color-accent-signal)]" /> : <Bell className="h-4 w-4" />}
        {count > 0 && (
          <span
            aria-hidden
            className="absolute right-1 top-1 inline-flex h-3.5 min-w-[14px] items-center justify-center rounded-full bg-[var(--color-accent-signal)] px-[3px] font-mono text-[9px] font-bold tabular-nums text-[var(--color-accent-signal-foreground)] ring-2 ring-[var(--color-background)]"
          >
            {count > 99 ? "99+" : count}
          </span>
        )}
      </button>

      {open && (
        <>
          {/* Click-away catcher — not in the tab order or AT tree. */}
          <button
            type="button"
            aria-hidden
            tabIndex={-1}
            onClick={() => setOpen(false)}
            className="fixed inset-0 z-40 cursor-default bg-transparent"
          />
          <div
            aria-label="Notifications"
            className="absolute right-0 z-50 mt-2 w-[22rem] overflow-hidden rounded-xl card-shell shadow-[0_24px_64px_-24px_oklch(0_0_0/0.30)]"
          >
            <div className="flex items-center justify-between border-b border-[var(--color-border)] px-3 py-2.5">
              <div className="meta text-[var(--color-muted-foreground)]">// Notifications</div>
              {count > 0 && (
                <button
                  type="button"
                  onClick={() => markAll.mutate()}
                  disabled={markAll.isPending}
                  className="inline-flex items-center gap-1 font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
                >
                  <CheckCheck className="h-3 w-3" />
                  Mark all read
                </button>
              )}
            </div>

            <div className="max-h-[24rem] overflow-y-auto">
              {recent.isLoading && (
                <p
                  role="status"
                  className="px-3 py-6 text-center font-mono text-xs uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]"
                >
                  Loading<span className="caret text-[var(--color-accent-signal)]" />
                </p>
              )}

              {!recent.isLoading && items.length === 0 && (
                <p className="px-3 py-8 text-center text-sm text-[var(--color-muted-foreground)]">
                  You're all caught up.
                </p>
              )}

              <ul className="divide-y divide-[var(--color-border)]">
                {items.map((n) => (
                  <Row
                    key={n.id}
                    notif={n}
                    onMarkRead={() => markOne.mutate(n.id)}
                    onClick={() => setOpen(false)}
                  />
                ))}
              </ul>
            </div>

            <div className="border-t border-[var(--color-border)] px-3 py-2">
              <Link
                to="/notifications"
                onClick={() => setOpen(false)}
                className="inline-flex items-center gap-1 font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-foreground)] hover:underline"
              >
                View all
              </Link>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

function Row({
  notif,
  onMarkRead,
  onClick,
}: {
  notif: NotificationDto;
  onMarkRead: () => void;
  onClick: () => void;
}) {
  const unread = !notif.readAtUtc;
  const body = (
    <div className="min-w-0 flex-1">
      <div className="flex flex-wrap items-baseline gap-x-2">
        <span className="truncate font-medium">{notif.title}</span>
        <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          {formatRelative(notif.createdAtUtc)}
        </span>
      </div>
      {notif.body && (
        <p className="mt-0.5 line-clamp-2 text-[12px] text-[var(--color-muted-foreground)]">
          {notif.body}
        </p>
      )}
    </div>
  );

  return (
    <li
      className={cn(
        "group/notif flex items-start gap-2.5 px-3 py-2.5 text-sm transition-colors",
        unread && "bg-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.04)]",
        "hover:bg-[var(--color-muted)]/40",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full",
          unread ? "bg-[var(--color-accent-signal)]" : "bg-transparent",
        )}
      />
      {notif.link ? (
        <Link to={notif.link} onClick={onClick} className="block min-w-0 flex-1">
          {body}
        </Link>
      ) : (
        body
      )}
      {unread && (
        <button
          type="button"
          onClick={(e) => {
            e.preventDefault();
            e.stopPropagation();
            onMarkRead();
          }}
          aria-label="Mark as read"
          className="invisible mt-0.5 inline-flex h-5 w-5 items-center justify-center rounded text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover/notif:visible"
        >
          <CheckCheck className="h-3 w-3" />
        </button>
      )}
    </li>
  );
}

function formatRelative(value: string): string {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  const diff = Date.now() - d.getTime();
  const sec = Math.round(diff / 1000);
  if (sec < 60) return `${sec}s`;
  const min = Math.round(sec / 60);
  if (min < 60) return `${min}m`;
  const hr = Math.round(min / 60);
  if (hr < 24) return `${hr}h`;
  const day = Math.round(hr / 24);
  if (day < 14) return `${day}d`;
  return d.toLocaleDateString();
}

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Bell, CheckCheck, ExternalLink, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import {
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  type NotificationDto,
} from "@/api/notifications";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  EntityPageHeader,
  ErrorBand,
  FilterBar,
  LoadingRow,
  Select,
} from "@/components/list";
import { EmptyState } from "@/components/empty-state";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

type Filter = "all" | "unread";

const FILTER_OPTIONS = [
  { value: "unread", label: "Unread" },
  { value: "all", label: "All" },
];

export function NotificationsInboxPage() {
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<Filter>("unread");

  const query = useQuery({
    queryKey: ["notifications", "inbox", filter],
    queryFn: () =>
      listNotifications({ unreadOnly: filter === "unread", pageSize: 100 }),
    staleTime: 15_000,
  });

  // Live append on new notification.
  useRealtimeEvent<unknown>("NotificationCreated", () => {
    queryClient.invalidateQueries({ queryKey: ["notifications"] });
  });

  const markOne = useMutation({
    mutationFn: (id: string) => markNotificationRead(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
    onError: (err) => toast.error("Mark read failed", { description: describe(err) }),
  });

  const markAll = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: (data) => {
      toast.success(`${data.updated} ${data.updated === 1 ? "notification" : "notifications"} marked read`);
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },
    onError: (err) => toast.error("Mark all failed", { description: describe(err) }),
  });

  const items = query.data ?? [];

  return (
    <div className="space-y-8">
      <EntityPageHeader
        icon={Bell}
        title="Notifications"
        total={items.length}
        unit="item"
        description="Events the system has surfaced for you. Live-updates as new notifications arrive — no refresh needed."
      >
        <Button
          variant="outline"
          size="sm"
          disabled={query.isFetching}
          onClick={() => query.refetch()}
          className="flex-1 sm:flex-none"
        >
          <RefreshCw className={cn("mr-1.5 h-3.5 w-3.5", query.isFetching && "animate-spin")} />
          Refresh
        </Button>
        <Button
          variant="signal"
          size="sm"
          onClick={() => markAll.mutate()}
          disabled={markAll.isPending}
          className="flex-1 sm:flex-none"
        >
          <CheckCheck className="mr-1.5 h-3.5 w-3.5" />
          {markAll.isPending ? "Marking…" : "Mark all read"}
        </Button>
      </EntityPageHeader>

      <FilterBar>
        <Select
          value={filter}
          onValueChange={(v) => setFilter((v as Filter) || "all")}
          options={FILTER_OPTIONS}
          className="min-w-[10rem]"
        />
      </FilterBar>

      {query.isError && (
        <ErrorBand
          message={
            query.error instanceof ApiRequestError
              ? query.error.problem?.detail ?? query.error.message
              : "Failed to load notifications."
          }
        />
      )}

      {query.isLoading && <LoadingRow label="Loading notifications" />}

      {!query.isLoading && items.length === 0 && !query.isError && (
        <EmptyState
          icon={Bell}
          kicker="// inbox zero"
          title={filter === "unread" ? "Nothing unread." : "No notifications yet."}
          description={
            filter === "unread"
              ? "You're all caught up. Live updates will pop in here as they fire."
              : "Notifications from the system will appear here as they're generated."
          }
        />
      )}

      {items.length > 0 && (
        <ul className="divide-y divide-[var(--color-border)] border-y border-[var(--color-border)]">
          {items.map((n) => (
            <Row key={n.id} notif={n} onMarkRead={() => markOne.mutate(n.id)} />
          ))}
        </ul>
      )}
    </div>
  );
}

function Row({
  notif,
  onMarkRead,
}: {
  notif: NotificationDto;
  onMarkRead: () => void;
}) {
  const unread = !notif.readAtUtc;
  return (
    <li
      className={cn(
        "grid grid-cols-[auto_auto_1fr_auto] items-start gap-3 px-1 py-3.5 text-sm",
        unread && "bg-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.03)]",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "mt-1.5 h-2 w-2 shrink-0 rounded-full",
          unread ? "bg-[var(--color-accent-signal)]" : "bg-transparent border border-[var(--color-border-strong)]",
        )}
      />
      <Badge variant="muted" className="font-mono uppercase tracking-[0.14em]">
        {notif.source}
      </Badge>
      <div className="min-w-0">
        <div className="flex flex-wrap items-baseline gap-x-2">
          <span className="font-medium">{notif.title}</span>
          <code className="code-chip">{notif.type}</code>
          <span className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            {new Date(notif.createdAtUtc).toLocaleString()}
          </span>
        </div>
        {notif.body && (
          <p className="mt-0.5 text-[13px] text-[var(--color-muted-foreground)]">
            {notif.body}
          </p>
        )}
        {notif.link && (
          <a
            href={notif.link}
            target={notif.link.startsWith("http") ? "_blank" : undefined}
            rel="noopener noreferrer"
            className="mt-1 inline-flex items-center gap-1 font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-foreground)] hover:underline"
          >
            <ExternalLink className="h-3 w-3" />
            Open
          </a>
        )}
      </div>
      {unread && (
        <Button variant="ghost" size="sm" onClick={onMarkRead}>
          <CheckCheck className="mr-1 h-3.5 w-3.5" /> Mark read
        </Button>
      )}
    </li>
  );
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return String(err);
}

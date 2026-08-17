import { useState } from "react";
import { useTranslation } from "react-i18next";
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
import { formatDate } from "@/lib/format";
import { cn } from "@/lib/cn";

type Filter = "all" | "unread";

export function NotificationsInboxPage() {
  const { t } = useTranslation("notifications");
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<Filter>("unread");

  const FILTER_OPTIONS = [
    { value: "unread", label: t("filter.unread") },
    { value: "all", label: t("filter.all") },
  ];

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
    onError: (err) => toast.error(t("toast.markReadFailed"), { description: describe(err) }),
  });

  const markAll = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: (data) => {
      toast.success(t("toast.marked", { count: data.updated }));
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },
    onError: (err) => toast.error(t("toast.markAllFailed"), { description: describe(err) }),
  });

  const items = query.data ?? [];

  return (
    <div className="space-y-8">
      <EntityPageHeader
        icon={Bell}
        title={t("inbox.title")}
        total={items.length}
        unit={t("inbox.unit")}
        description={t("inbox.description")}
      >
        <Button
          variant="outline"
          size="sm"
          disabled={query.isFetching}
          onClick={() => query.refetch()}
          className="flex-1 sm:flex-none"
        >
          <RefreshCw className={cn("mr-1.5 h-3.5 w-3.5", query.isFetching && "animate-spin")} />
          {t("inbox.refresh")}
        </Button>
        <Button
          variant="signal"
          size="sm"
          onClick={() => markAll.mutate()}
          disabled={markAll.isPending}
          className="flex-1 sm:flex-none"
        >
          <CheckCheck className="mr-1.5 h-3.5 w-3.5" />
          {markAll.isPending ? t("inbox.marking") : t("inbox.markAllRead")}
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
              : t("inbox.loadError")
          }
        />
      )}

      {query.isLoading && <LoadingRow label={t("inbox.loading")} />}

      {!query.isLoading && items.length === 0 && !query.isError && (
        <EmptyState
          icon={Bell}
          kicker={t("inbox.empty.kicker")}
          title={filter === "unread" ? t("inbox.empty.unreadTitle") : t("inbox.empty.allTitle")}
          description={
            filter === "unread"
              ? t("inbox.empty.unreadDesc")
              : t("inbox.empty.allDesc")
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
  const { t } = useTranslation("notifications");
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
            {formatDate(notif.createdAtUtc)}
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
            {t("row.open")}
          </a>
        )}
      </div>
      {unread && (
        <Button variant="ghost" size="sm" onClick={onMarkRead}>
          <CheckCheck className="mr-1 h-3.5 w-3.5" /> {t("row.markRead")}
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

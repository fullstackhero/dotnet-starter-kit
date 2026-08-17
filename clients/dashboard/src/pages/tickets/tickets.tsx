import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { Link } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  AlertTriangle,
  ChevronRight,
  Plus,
  Ticket as TicketIcon,
} from "lucide-react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import {
  createTicket,
  searchTickets,
  TICKET_PRIORITIES,
  TICKET_STATUSES,
  type CreateTicketInput,
  type TicketDto,
  type TicketPriority,
  type TicketStatus,
} from "@/api/tickets";
import {
  PRIORITY_LABEL_KEY,
  PRIORITY_TONE,
  STATUS_LABEL_KEY,
  STATUS_TONE,
} from "@/lib/ticket-enums";
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
import { Input } from "@/components/ui/input";
import {
  Combobox,
  EntityEmpty,
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityPager,
  EntitySearch,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe, formatRelative } from "@/lib/list-helpers";
import { useUserDisplay } from "@/lib/use-user-display";

const PAGE_SIZE = 20;

type EditorState = { mode: "closed" } | { mode: "create" };

// ─── Grid template — used by header, rows, and the loading skeleton ──────

const DESKTOP_GRID =
  "grid-cols-[1fr_100px_120px_140px_110px_24px] lg:grid-cols-[1fr_110px_140px_160px_120px_24px]";

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function TicketsPage() {
  const { t } = useTranslation("tickets");
  const queryClient = useQueryClient();

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<TicketStatus | null>(null);
  const [priorityFilter, setPriorityFilter] = useState<TicketPriority | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  // Debounce search
  useEffect(() => {
    const id = window.setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => window.clearTimeout(id);
  }, [search]);

  useEffect(() => setPageNumber(1), [debouncedSearch, statusFilter, priorityFilter]);

  const query = useQuery({
    queryKey: [
      "tickets",
      "list",
      { search: debouncedSearch, statusFilter, priorityFilter, pageNumber },
    ],
    queryFn: () =>
      searchTickets({
        search: debouncedSearch || undefined,
        status: statusFilter ?? undefined,
        priority: priorityFilter ?? undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy: "createdAtUtc",
        sortDir: "desc",
      }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];

  const filtersApplied = statusFilter !== null || priorityFilter !== null;
  const searchActive = debouncedSearch.length > 0 || filtersApplied;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={TicketIcon}
        title={t("list.title")}
        total={data?.totalCount ?? null}
        unit={t("list.unit")}
        description={t("list.description")}
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("list.new")}
        </Button>
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder={t("list.searchPlaceholder")}
      />

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-2">
        <EntityFilterPill<TicketStatus | null>
          label={t("filter.status")}
          value={statusFilter}
          onChange={setStatusFilter}
          options={[
            { value: null, label: t("filter.all") },
            ...TICKET_STATUSES.map((s) => ({
              value: s,
              label: t(STATUS_LABEL_KEY[s]),
            })),
          ]}
        />
        <EntityFilterPill<TicketPriority | null>
          label={t("filter.priority")}
          value={priorityFilter}
          onChange={setPriorityFilter}
          options={[
            { value: null, label: t("filter.any") },
            ...TICKET_PRIORITIES.map((p) => ({
              value: p,
              label: t(PRIORITY_LABEL_KEY[p]),
            })),
          ]}
        />
      </div>

      {/* Results */}
      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_GRID} rows={6} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={TicketIcon}
          title={searchActive ? t("empty.searchTitle") : t("empty.title")}
          body={
            searchActive
              ? debouncedSearch
                ? t("empty.searchBodyTerm", { term: debouncedSearch })
                : t("empty.searchBody")
              : t("empty.body")
          }
          action={
            searchActive ? (
              <Button
                variant="outline"
                onClick={() => {
                  setSearch("");
                  setStatusFilter(null);
                  setPriorityFilter(null);
                }}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                {t("empty.clearFilters")}
              </Button>
            ) : (
              <Button
                onClick={() => setEditor({ mode: "create" })}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                <Plus className="mr-1.5 size-4" />
                {t("empty.openTicket")}
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {t("list.count", { count: data?.totalCount ?? 0 })}
            </p>
          </div>

          {/* Mobile: card list */}
          <div className="space-y-2 md:hidden">
            {items.map((ticket) => (
              <MobileCard key={ticket.id} ticket={ticket} />
            ))}
          </div>

          {/* Desktop: table */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_GRID}>
              <span>{t("col.subject")}</span>
              <span>{t("col.priority")}</span>
              <span>{t("col.status")}</span>
              <span>{t("col.assignee")}</span>
              <span>{t("col.updated")}</span>
              <span />
            </EntityListHeader>
            {items.map((ticket, i) => (
              <DesktopRow
                key={ticket.id}
                ticket={ticket}
                isLast={i === items.length - 1}
              />
            ))}
          </EntityListCard>

          <EntityPager
            page={data?.pageNumber ?? pageNumber}
            totalPages={Math.max(data?.totalPages ?? 1, 1)}
            hasPrev={data?.hasPrevious ?? false}
            hasNext={data?.hasNext ?? false}
            onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
            onNext={() => setPageNumber((p) => p + 1)}
          />
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <AlertTriangle className="mt-0.5 size-4 shrink-0" />
          <span>{describe(query.error)}</span>
        </div>
      )}

      <CreateTicketDialog
        open={editor.mode === "create"}
        onClose={() => setEditor({ mode: "closed" })}
        onCreated={() => {
          void queryClient.invalidateQueries({ queryKey: ["tickets"] });
        }}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Mobile card — avatar of the reporter + title/number + status badges
// ───────────────────────────────────────────────────────────────────────

function MobileCard({ ticket }: { ticket: TicketDto }) {
  const { t } = useTranslation("tickets");
  return (
    <Link
      to={`/tickets/${ticket.id}`}
      aria-label={t("aria.openTicket", { title: ticket.title })}
      className={cn(
        "block rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
        "shadow-xs",
        "transition-colors hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)] active:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.6)]",
        "outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.4)]",
      )}
    >
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={ticket.reporterUserId} size={40} />
          <div className="min-w-0">
            <div className="flex items-center gap-1.5">
              <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
                {ticket.title}
              </p>
            </div>
            <div className="mt-0.5 flex items-center gap-1.5">
              <code className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
                {ticket.number}
              </code>
            </div>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-2">
        <EntityStatusBadge tone={PRIORITY_TONE[ticket.priority]}>
          {t(PRIORITY_LABEL_KEY[ticket.priority])}
        </EntityStatusBadge>
        <EntityStatusBadge tone={STATUS_TONE[ticket.status]}>
          {t(STATUS_LABEL_KEY[ticket.status])}
        </EntityStatusBadge>
        <span className="ml-auto font-mono text-[11px] text-[var(--color-muted-foreground)]">
          {formatRelative(ticket.updatedAtUtc ?? ticket.createdAtUtc)}
        </span>
      </div>
    </Link>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Desktop row
// ───────────────────────────────────────────────────────────────────────

function DesktopRow({
  ticket,
  isLast,
}: {
  ticket: TicketDto;
  isLast: boolean;
}) {
  const { t } = useTranslation("tickets");
  const assignee = useUserDisplay(ticket.assignedToUserId);
  return (
    <EntityListRow className={DESKTOP_GRID} isLast={isLast}>
      {/* Subject — avatar + title + number */}
      <Link
        to={`/tickets/${ticket.id}`}
        className="flex min-w-0 items-center gap-3 outline-none"
      >
        <EntityInitialsAvatar name={ticket.reporterUserId} size={36} />
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {ticket.title}
          </span>
          <code className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
            {ticket.number}
          </code>
        </div>
      </Link>

      {/* Priority */}
      <span>
        <EntityStatusBadge tone={PRIORITY_TONE[ticket.priority]}>
          {t(PRIORITY_LABEL_KEY[ticket.priority])}
        </EntityStatusBadge>
      </span>

      {/* Status */}
      <span>
        <EntityStatusBadge tone={STATUS_TONE[ticket.status]}>
          {t(STATUS_LABEL_KEY[ticket.status])}
        </EntityStatusBadge>
      </span>

      {/* Assignee */}
      <div className="flex min-w-0 items-center gap-2">
        {ticket.assignedToUserId ? (
          <>
            <EntityInitialsAvatar name={assignee.name} size={24} />
            <span
              title={assignee.handle ?? ticket.assignedToUserId}
              className="truncate text-[12px] text-[var(--color-foreground)]"
            >
              {assignee.name}
            </span>
          </>
        ) : (
          <span className="font-mono text-[11px] uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]">
            {t("row.unassigned")}
          </span>
        )}
      </div>

      {/* Updated */}
      <span className="text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
        {formatRelative(ticket.updatedAtUtc ?? ticket.createdAtUtc)}
      </span>

      {/* Trailing chevron */}
      <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
    </EntityListRow>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Create dialog
// ───────────────────────────────────────────────────────────────────────

function CreateTicketDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: () => void;
}) {
  const { t } = useTranslation("tickets");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<TicketPriority>("Medium");

  useEffect(() => {
    if (open) {
      setTitle("");
      setDescription("");
      setPriority("Medium");
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: (input: CreateTicketInput) => createTicket(input),
    onSuccess: () => {
      toast.success(t("create.toastOpened"));
      onCreated();
      onClose();
    },
    onError: (err: unknown) => {
      toast.error(describe(err));
    },
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!title.trim()) return;
    mutation.mutate({
      title: title.trim(),
      description: description.trim() || null,
      priority,
    });
  };

  const priorityOptions = useMemo(
    () =>
      TICKET_PRIORITIES.map((p) => ({
        value: p,
        label: t(PRIORITY_LABEL_KEY[p]),
      })),
    [t],
  );

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <TicketIcon className="size-4 text-[var(--color-primary)]" />
              {t("create.title")}
            </DialogTitle>
            <DialogDescription>
              {t("create.descPrefix")}
              <code className="font-mono text-[11px]">{t("status.Open")}</code>
              {t("create.descSuffix")}
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <Field id="ticket-title" label={t("create.fieldTitle")} required>
              <Input
                id="ticket-title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder={t("create.placeholderTitle")}
                maxLength={160}
                autoFocus
                required
              />
            </Field>
            <Field id="ticket-description" label={t("create.fieldDescription")}>
              <textarea
                id="ticket-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder={t("create.placeholderDescription")}
                rows={4}
                className={cn(
                  "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
                  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
                  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
                )}
                maxLength={4096}
              />
            </Field>
            <Field id="ticket-priority" label={t("create.fieldPriority")}>
              <Combobox
                id="ticket-priority"
                variant="field"
                label={t("create.fieldPriority")}
                value={priority}
                onChange={(v) => setPriority((v as TicketPriority) ?? "Medium")}
                options={priorityOptions}
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                {t("create.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={!title.trim() || mutation.isPending}>
              {mutation.isPending ? t("create.opening") : t("create.submit")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

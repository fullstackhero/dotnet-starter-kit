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
  AlertOctagon,
  AlertTriangle,
  CheckCircle2,
  CircleDot,
  Clock,
  MessageCircle,
  Plus,
  Search,
  Sparkles,
  Ticket as TicketIcon,
  Trash2,
  X,
} from "lucide-react";
import { toast } from "sonner";
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
import { Skeleton } from "@/components/ui/skeleton";
import {
  Combobox,
  DensityToggle,
  EmptyState,
  ErrorBand,
  Field,
  ListHero,
  Pagination,
  SortChips,
  Stat,
  StatStrip,
  usePersistedDensity,
  type Density,
  type SortDir,
  type SortOption,
} from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";
import {
  describe,
  formatRelative,
  pad2,
} from "@/lib/list-helpers";

const PAGE_SIZE = 20;
const DENSITY_KEY = "fsh.dashboard.tickets.density";

type SortKey = "createdAtUtc" | "number" | "priority" | "status" | "title";

const SORT_OPTIONS: SortOption<SortKey>[] = [
  { key: "createdAtUtc", label: "Created" },
  { key: "number", label: "Number" },
  { key: "priority", label: "Priority" },
  { key: "status", label: "Status" },
  { key: "title", label: "Title" },
];

type EditorState = { mode: "closed" } | { mode: "create" };

// ─── Status / Priority tone tables ──────────────────────────────────────
//
// Keep these in one place. The "tone" is the OKLCH-tinted accent that
// drives the row's left-edge pull-tab, the badge surface, and the icon
// fill. Mapping is centralized so changes ripple consistently.

type Tone = "open" | "progress" | "resolved" | "closed";

const STATUS_TONE: Record<TicketStatus, Tone> = {
  Open: "open",
  InProgress: "progress",
  Resolved: "resolved",
  Closed: "closed",
};

const STATUS_COLOR: Record<Tone, string> = {
  open: "var(--color-primary)",
  progress: "oklch(0.700 0.155 195)", // cyan
  resolved: "var(--color-success)",
  closed: "var(--color-muted-foreground)",
};

const STATUS_LABEL: Record<TicketStatus, string> = {
  Open: "Open",
  InProgress: "In progress",
  Resolved: "Resolved",
  Closed: "Closed",
};

const STATUS_ICON: Record<TicketStatus, React.ComponentType<{ className?: string }>> = {
  Open: CircleDot,
  InProgress: Clock,
  Resolved: CheckCircle2,
  Closed: CheckCircle2,
};

const PRIORITY_LABEL: Record<TicketPriority, string> = {
  Low: "Low",
  Medium: "Medium",
  High: "High",
  Critical: "Critical",
};

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function TicketsPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<TicketStatus | null>(null);
  const [priorityFilter, setPriorityFilter] = useState<TicketPriority | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [density, setDensity] = usePersistedDensity(DENSITY_KEY);
  const [sortKey, setSortKey] = useState<SortKey>("createdAtUtc");
  const [sortDir, setSortDir] = useState<SortDir>("desc");
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const onSort = (next: SortKey) => {
    if (sortKey === next) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(next);
      setSortDir(next === "createdAtUtc" || next === "priority" ? "desc" : "asc");
    }
  };

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
      { search: debouncedSearch, statusFilter, priorityFilter, pageNumber, sortKey, sortDir },
    ],
    queryFn: () =>
      searchTickets({
        search: debouncedSearch || undefined,
        status: statusFilter ?? undefined,
        priority: priorityFilter ?? undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy: sortKey,
        sortDir,
      }),
    placeholderData: keepPreviousData,
  });

  const items = query.data?.items ?? [];

  const stats = useMemo(() => {
    const buckets = { open: 0, progress: 0, resolved: 0, critical: 0 };
    for (const t of items) {
      if (t.status === "Open") buckets.open++;
      else if (t.status === "InProgress") buckets.progress++;
      else if (t.status === "Resolved") buckets.resolved++;
      if (t.priority === "Critical") buckets.critical++;
    }
    return buckets;
  }, [items]);

  const filtersApplied = statusFilter !== null || priorityFilter !== null;

  return (
    <div className="space-y-6 pb-12">
      <ListHero
        eyebrow="Helpdesk · Tickets"
        tenant={user?.tenant ?? "—"}
        subEyebrow={query.data ? `page ${pad2(pageNumber)}` : undefined}
        title="Tickets"
        totalCount={query.data?.totalCount ?? null}
        subtitle="Open work, ranked by priority. The desk where issues land, get owned, and ship."
        searchValue={search}
        onSearch={setSearch}
        searchPlaceholder="Find by number, title, or description…"
        isFetching={query.isFetching}
        onRefresh={() => void query.refetch()}
        ctaLabel="New ticket"
        onCreate={() => setEditor({ mode: "create" })}
      />

      <StatStrip cols={4}>
        <Stat
          label="Open"
          value={query.isLoading ? "—" : stats.open.toString()}
          hint="Unassigned or awaiting triage"
          accent
        />
        <Stat
          label="In progress"
          value={query.isLoading ? "—" : stats.progress.toString()}
          hint="Owned and moving"
        />
        <Stat
          label="Resolved"
          value={query.isLoading ? "—" : stats.resolved.toString()}
          hint="Awaiting close"
        />
        <Stat
          label="Critical"
          value={query.isLoading ? "—" : stats.critical.toString()}
          hint="Highest-priority open work"
          tone={stats.critical > 0 ? "danger" : "default"}
        />
      </StatStrip>

      <FilterBar
        statusFilter={statusFilter}
        priorityFilter={priorityFilter}
        onStatus={setStatusFilter}
        onPriority={setPriorityFilter}
        filtersApplied={filtersApplied}
        onClearAll={() => {
          setStatusFilter(null);
          setPriorityFilter(null);
        }}
        sortKey={sortKey}
        sortDir={sortDir}
        onSort={onSort}
        density={density}
        onDensity={setDensity}
      />

      {query.isError && <ErrorBand message={describe(query.error)} />}

      <section
        aria-label="Ticket list"
        className={cn(
          "fsh-enter fsh-enter-3 card-shell overflow-hidden rounded-2xl",
          "bg-[var(--color-surface-3)]",
        )}
      >
        {query.isLoading && items.length === 0 ? (
          <ul aria-busy>
            {Array.from({ length: 5 }).map((_, i) => (
              <SkeletonRow key={i} delayMs={i * 40} density={density} />
            ))}
          </ul>
        ) : items.length === 0 ? (
          (() => {
            const filtered = debouncedSearch.length > 0 || filtersApplied;
            return (
              <EmptyState
                eyebrow={filtered ? "No matches" : "Empty desk"}
                headline={
                  filtered
                    ? debouncedSearch
                      ? `Nothing matches "${debouncedSearch}".`
                      : "No tickets match the current filters."
                    : "Your tenant has no open tickets."
                }
                body={
                  filtered
                    ? "Try widening the filter set, or clear everything and start fresh."
                    : "Create the first ticket to start tracking work. Tickets carry a status, priority, an optional assignee, and a comment thread."
                }
                icon={
                  filtered ? (
                    <Search className="h-6 w-6 text-[var(--color-primary)]" />
                  ) : (
                    <TicketIcon className="h-6 w-6 text-[var(--color-primary)]" />
                  )
                }
                primaryAction={{
                  label: filtered ? "Open a new ticket" : "Open the first ticket",
                  onClick: () => setEditor({ mode: "create" }),
                  icon: <Sparkles className="h-3.5 w-3.5" />,
                }}
                secondaryAction={
                  filtered
                    ? {
                        label: "Clear filters",
                        onClick: () => {
                          setSearch("");
                          setStatusFilter(null);
                          setPriorityFilter(null);
                        },
                        icon: <X className="h-3.5 w-3.5" />,
                      }
                    : undefined
                }
              />
            );
          })()
        ) : (
          <ul role="list">
            {items.map((ticket, i) => (
              <Row
                key={ticket.id}
                ticket={ticket}
                density={density}
                delayMs={Math.min(i, 8) * 30}
              />
            ))}
          </ul>
        )}
      </section>

      {query.data && query.data.totalCount > 0 && (
        <Pagination
          page={query.data.pageNumber}
          totalPages={Math.max(query.data.totalPages, 1)}
          totalCount={query.data.totalCount}
          shown={items.length}
          fetching={query.isFetching}
          hasPrev={query.data.hasPrevious}
          hasNext={query.data.hasNext}
          onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
          onNext={() => setPageNumber((p) => p + 1)}
        />
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
//  FilterBar — status + priority comboboxes, sort chips, density toggle
// ───────────────────────────────────────────────────────────────────────

function FilterBar({
  statusFilter,
  priorityFilter,
  onStatus,
  onPriority,
  filtersApplied,
  onClearAll,
  sortKey,
  sortDir,
  onSort,
  density,
  onDensity,
}: {
  statusFilter: TicketStatus | null;
  priorityFilter: TicketPriority | null;
  onStatus: (v: TicketStatus | null) => void;
  onPriority: (v: TicketPriority | null) => void;
  filtersApplied: boolean;
  onClearAll: () => void;
  sortKey: SortKey;
  sortDir: SortDir;
  onSort: (k: SortKey) => void;
  density: Density;
  onDensity: (v: Density) => void;
}) {
  return (
    <div className="fsh-enter fsh-enter-2 flex flex-wrap items-center gap-3">
      <Combobox
        variant="filter"
        label="Status"
        value={statusFilter}
        onChange={(v) => onStatus(v as TicketStatus | null)}
        clearable
        emptyOptionLabel="All statuses"
        options={TICKET_STATUSES.map((s) => ({
          value: s,
          label: STATUS_LABEL[s],
          prefix: <span className="h-2 w-2 rounded-full" style={{ backgroundColor: STATUS_COLOR[STATUS_TONE[s]] }} />,
        }))}
      />
      <Combobox
        variant="filter"
        label="Priority"
        value={priorityFilter}
        onChange={(v) => onPriority(v as TicketPriority | null)}
        clearable
        emptyOptionLabel="All priorities"
        options={TICKET_PRIORITIES.map((p) => ({
          value: p,
          label: PRIORITY_LABEL[p],
        }))}
      />

      {filtersApplied && (
        <button
          type="button"
          onClick={onClearAll}
          className={cn(
            "inline-flex h-7 cursor-pointer items-center gap-1 rounded-full px-2.5",
            "font-mono text-[10.5px] font-medium uppercase tracking-[0.14em]",
            "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
            "transition-colors hover:bg-[var(--color-surface-4)] hover:text-[var(--color-foreground)]",
          )}
        >
          <X className="h-3 w-3" />
          Clear
        </button>
      )}

      <span aria-hidden className="hidden h-5 w-px bg-[var(--color-border)] sm:inline-block" />

      <SortChips
        prefixLabel="desk"
        sortKey={sortKey}
        sortDir={sortDir}
        onSort={onSort}
        options={SORT_OPTIONS}
      />

      <div className="ml-auto">
        <DensityToggle density={density} onChange={onDensity} />
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Row — the stub card on the desk
// ───────────────────────────────────────────────────────────────────────

function Row({
  ticket,
  density,
  delayMs,
}: {
  ticket: TicketDto;
  density: Density;
  delayMs: number;
}) {
  const tone = STATUS_TONE[ticket.status];
  const StatusIcon = STATUS_ICON[ticket.status];
  const isCompact = density === "compact";

  return (
    <li
      className={cn(
        "fsh-enter group/row relative flex items-stretch gap-4 border-t border-[var(--color-border)]",
        "first:border-t-0 transition-colors duration-[var(--duration-fast)]",
        "hover:bg-[var(--color-surface-4)]",
        isCompact ? "px-5 py-3" : "px-6 py-4",
      )}
      style={{ animationDelay: `${delayMs}ms` }}
    >
      {/* Status pull-tab — colored bar on the very left edge */}
      <span
        aria-hidden
        className={cn(
          "absolute inset-y-2.5 left-0 w-[3px] rounded-r-full",
          "transition-opacity duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        )}
        style={{ backgroundColor: STATUS_COLOR[tone] }}
      />

      {/* Identity column — number, title, description preview, priority + status chip */}
      <div className="flex min-w-0 flex-1 flex-col gap-1.5 pl-2">
        <div className="flex flex-wrap items-baseline gap-x-2.5 gap-y-1">
          <code
            title={ticket.number}
            className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px] font-medium tracking-tight text-[var(--color-muted-foreground)]"
          >
            {ticket.number}
          </code>
          <Link
            to={`/tickets/${ticket.id}`}
            className={cn(
              "text-display truncate text-[15.5px] font-semibold leading-tight tracking-[-0.01em] sm:text-[16px]",
              "decoration-[var(--color-border-strong)] decoration-1 underline-offset-[5px]",
              "transition-colors duration-[var(--duration-fast)]",
              "hover:text-[var(--color-primary)] hover:underline focus-visible:underline",
              "focus-visible:outline-none focus-visible:text-[var(--color-primary)]",
            )}
          >
            {ticket.title}
          </Link>
          <PriorityFlag priority={ticket.priority} />
        </div>

        {ticket.description && !isCompact && (
          <p className="line-clamp-1 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]">
            {ticket.description}
          </p>
        )}

        <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[11.5px] text-[var(--color-muted-foreground)]">
          <span
            className={cn(
              "inline-flex items-center gap-1 rounded-full px-2 py-0.5",
              "font-mono text-[10px] font-medium uppercase tracking-[0.14em]",
            )}
            style={{
              backgroundColor: `oklch(from ${STATUS_COLOR[tone]} l c h / 0.10)`,
              color: STATUS_COLOR[tone],
            }}
          >
            <StatusIcon className="h-3 w-3" />
            {STATUS_LABEL[ticket.status]}
          </span>
          {ticket.commentCount > 0 && (
            <span className="inline-flex items-center gap-1">
              <MessageCircle className="h-3 w-3" />
              {ticket.commentCount}
            </span>
          )}
          <span className="inline-flex items-center gap-1">
            <span className="font-mono text-[10px] uppercase tracking-[0.14em] opacity-70">
              created
            </span>
            <span className="tabular-nums">{formatRelative(ticket.createdAtUtc)}</span>
          </span>
          {ticket.updatedAtUtc && (
            <span className="hidden items-center gap-1 sm:inline-flex">
              <span className="font-mono text-[10px] uppercase tracking-[0.14em] opacity-70">
                updated
              </span>
              <span className="tabular-nums">{formatRelative(ticket.updatedAtUtc)}</span>
            </span>
          )}
        </div>
      </div>

      {/* Right column — assignee/reporter avatars */}
      <div className="hidden shrink-0 flex-col items-end justify-center gap-2 sm:flex">
        <PeopleStack
          assignedToUserId={ticket.assignedToUserId}
          reporterUserId={ticket.reporterUserId}
        />
      </div>
    </li>
  );
}

function PriorityFlag({ priority }: { priority: TicketPriority }) {
  const map: Record<
    TicketPriority,
    { color: string; weight: string; label: string }
  > = {
    Low: { color: "var(--color-muted-foreground)", weight: "▪", label: "low" },
    Medium: { color: "oklch(0.700 0.155 195)", weight: "▪▪", label: "med" },
    High: { color: "var(--color-warning)", weight: "▪▪▪", label: "high" },
    Critical: { color: "var(--color-destructive)", weight: "▪▪▪▪", label: "crit" },
  };
  const m = map[priority];
  return (
    <span
      title={`Priority: ${PRIORITY_LABEL[priority]}`}
      className={cn(
        "inline-flex items-center gap-1 rounded-full px-1.5 py-0.5",
        "font-mono text-[9.5px] font-medium uppercase tracking-[0.14em]",
      )}
      style={{
        backgroundColor: `oklch(from ${m.color} l c h / 0.10)`,
        color: m.color,
      }}
    >
      {priority === "Critical" && <AlertOctagon className="h-2.5 w-2.5" />}
      {priority === "High" && <AlertTriangle className="h-2.5 w-2.5" />}
      {m.label}
    </span>
  );
}

function PeopleStack({
  assignedToUserId,
  reporterUserId,
}: {
  assignedToUserId: string | null | undefined;
  reporterUserId: string;
}) {
  return (
    <div className="flex items-center gap-2">
      {assignedToUserId ? (
        <Avatar id={assignedToUserId} role="Assignee" />
      ) : (
        <UnassignedBadge />
      )}
      <span aria-hidden className="h-px w-3 bg-[var(--color-border-strong)]" />
      <Avatar id={reporterUserId} role="Reporter" muted />
    </div>
  );
}

function Avatar({ id, role, muted }: { id: string; role: string; muted?: boolean }) {
  const initial = id.replace(/[^a-z0-9]/gi, "").charAt(0).toUpperCase() || "?";
  return (
    <span
      title={`${role}: ${id}`}
      className={cn(
        "grid h-6 w-6 place-items-center rounded-full",
        "font-mono text-[10px] font-semibold tracking-tight",
        muted
          ? "bg-[var(--color-muted)] text-[var(--color-muted-foreground)] ring-1 ring-inset ring-[var(--color-border)]"
          : "bg-[var(--color-primary-soft)] text-[var(--color-primary)] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.30)]",
      )}
    >
      {initial}
    </span>
  );
}

function UnassignedBadge() {
  return (
    <span
      title="Unassigned"
      className={cn(
        "grid h-6 w-6 place-items-center rounded-full",
        "border border-dashed border-[var(--color-border-strong)]",
        "text-[var(--color-muted-foreground)]",
      )}
    >
      <span className="text-[10px]">·</span>
    </span>
  );
}

function SkeletonRow({ delayMs, density }: { delayMs: number; density: Density }) {
  const isCompact = density === "compact";
  return (
    <li
      className={cn(
        "fsh-enter flex items-stretch gap-4 border-t border-[var(--color-border)] first:border-t-0",
        isCompact ? "px-5 py-3" : "px-6 py-4",
      )}
      style={{ animationDelay: `${delayMs}ms` }}
    >
      <Skeleton className="h-9 w-1" />
      <div className="flex-1 space-y-2">
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="h-3 w-1/2" />
      </div>
      <Skeleton className="h-7 w-7 rounded-full" />
    </li>
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
      toast.success("Ticket opened");
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

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <DialogHeader>
          <div className="flex items-center gap-2">
            <span
              aria-hidden
              className="grid h-6 w-6 place-items-center rounded-md bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            >
              <TicketIcon className="h-3.5 w-3.5" />
            </span>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              New ticket
            </span>
          </div>
          <DialogTitle>Open a ticket</DialogTitle>
          <DialogDescription>
            Tickets land on the desk as <code className="font-mono">Open</code>. Assign one
            to start it; resolve it with a note when work is done.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={onSubmit}>
          <DialogBody className="space-y-4">
            <Field id="ticket-title" label="Title" required>
              <Input
                id="ticket-title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Briefly describe the issue or request"
                maxLength={160}
                autoFocus
                required
              />
            </Field>
            <Field id="ticket-description" label="Description">
              <textarea
                id="ticket-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Steps to reproduce, expected behavior, anything else useful"
                rows={4}
                className={cn(
                  "block w-full rounded-md border border-[var(--color-input)] bg-[var(--color-surface-2)]",
                  "px-3 py-2 text-sm shadow-[var(--shadow-xs)] placeholder:text-[var(--color-muted-foreground)]",
                  "transition-[border-color,box-shadow,background-color] duration-[var(--duration-fast)]",
                  "focus-visible:border-[var(--color-input)]",
                )}
                maxLength={4096}
              />
            </Field>
            <Field id="ticket-priority" label="Priority">
              <Combobox
                id="ticket-priority"
                variant="field"
                label="Priority"
                value={priority}
                onChange={(v) => setPriority((v as TicketPriority) ?? "Medium")}
                options={TICKET_PRIORITIES.map((p) => ({
                  value: p,
                  label: PRIORITY_LABEL[p],
                }))}
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={!title.trim() || mutation.isPending}
              className="brand-glow gradient-sheen gap-1.5"
            >
              {mutation.isPending ? (
                "Opening…"
              ) : (
                <>
                  <Plus className="h-3.5 w-3.5" />
                  Open ticket
                </>
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// (Trash2 is imported for symmetry with other list pages — re-exported on demand.)
void Trash2;

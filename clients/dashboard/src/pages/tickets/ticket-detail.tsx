import {
  useEffect,
  useState,
  type FormEvent,
} from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  AlertOctagon,
  AlertTriangle,
  ArrowLeft,
  CheckCircle2,
  ChevronRight,
  CircleDot,
  Clock,
  MessageCircle,
  RefreshCw,
  RotateCcw,
  Send,
  Sparkles,
  Ticket as TicketIcon,
  UserCheck,
  UserX,
} from "lucide-react";
import { toast } from "sonner";
import {
  addTicketComment,
  assignTicket,
  getTicketById,
  listTicketComments,
  reopenTicket,
  resolveTicket,
  TICKET_PRIORITIES,
  type TicketDto,
  type TicketPriority,
  type TicketStatus,
} from "@/api/tickets";
import { UserPicker } from "@/components/identity/user-picker";
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
import { Skeleton } from "@/components/ui/skeleton";
import { ErrorBand, Field } from "@/components/list";
import { cn } from "@/lib/cn";
import {
  describe,
  formatDate,
  formatDateMono,
  formatRelative,
} from "@/lib/list-helpers";

type DialogState =
  | { mode: "closed" }
  | { mode: "resolve" }
  | { mode: "assign" };

// ─── Tone tables (mirror tickets.tsx) ─────────────────────────────────

type Tone = "open" | "progress" | "resolved" | "closed";

const STATUS_TONE: Record<TicketStatus, Tone> = {
  Open: "open",
  InProgress: "progress",
  Resolved: "resolved",
  Closed: "closed",
};

const STATUS_COLOR: Record<Tone, string> = {
  open: "var(--color-primary)",
  progress: "oklch(0.700 0.155 195)",
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

export function TicketDetailPage() {
  const { ticketId = "" } = useParams<{ ticketId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [dialog, setDialog] = useState<DialogState>({ mode: "closed" });

  const ticketQuery = useQuery({
    queryKey: ["tickets", "detail", ticketId],
    queryFn: () => getTicketById(ticketId),
    enabled: !!ticketId,
  });

  const commentsQuery = useQuery({
    queryKey: ["tickets", "comments", ticketId],
    queryFn: () => listTicketComments(ticketId),
    enabled: !!ticketId,
  });

  const ticket = ticketQuery.data;

  return (
    <div className="space-y-6 pb-12">
      <Breadcrumb
        ticketNumber={ticket?.number}
        onBack={() => navigate("/tickets")}
      />

      {ticketQuery.isError && <ErrorBand message={describe(ticketQuery.error)} />}

      {ticketQuery.isLoading ? (
        <DetailSkeleton />
      ) : ticket ? (
        <>
          <Hero
            ticket={ticket}
            isFetching={ticketQuery.isFetching}
            onRefresh={() => void ticketQuery.refetch()}
            onResolve={() => setDialog({ mode: "resolve" })}
            onReopen={() => {
              void (async () => {
                try {
                  await reopenTicket(ticket.id);
                  toast.success("Ticket reopened");
                  await queryClient.invalidateQueries({ queryKey: ["tickets"] });
                } catch (e) {
                  toast.error(describe(e));
                }
              })();
            }}
            onAssign={() => setDialog({ mode: "assign" })}
          />

          <div className="grid grid-cols-1 gap-5 lg:grid-cols-[1fr_320px]">
            <div className="space-y-5">
              <DescriptionPanel ticket={ticket} />
              <CommentsPanel
                ticketId={ticket.id}
                comments={commentsQuery.data ?? []}
                isLoading={commentsQuery.isLoading}
                onPosted={() => {
                  void queryClient.invalidateQueries({ queryKey: ["tickets", "comments", ticket.id] });
                  void queryClient.invalidateQueries({ queryKey: ["tickets", "detail", ticket.id] });
                }}
                disabled={ticket.status === "Closed"}
              />
            </div>
            <MetadataPanel ticket={ticket} />
          </div>

          <ResolveDialog
            open={dialog.mode === "resolve"}
            ticket={ticket}
            onClose={() => setDialog({ mode: "closed" })}
          />
          <AssignDialog
            open={dialog.mode === "assign"}
            ticket={ticket}
            onClose={() => setDialog({ mode: "closed" })}
          />
        </>
      ) : (
        <NotFoundPanel />
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Breadcrumb
// ───────────────────────────────────────────────────────────────────────

function Breadcrumb({
  ticketNumber,
  onBack,
}: {
  ticketNumber: string | undefined;
  onBack: () => void;
}) {
  return (
    <div className="fsh-enter fsh-enter-1 flex items-center justify-between gap-3">
      <nav
        aria-label="Breadcrumb"
        className="flex flex-wrap items-center gap-1.5 font-mono text-[10.5px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]"
      >
        <Link
          to="/tickets"
          className="rounded px-1.5 py-0.5 transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
        >
          Helpdesk
        </Link>
        <ChevronRight className="h-3 w-3 opacity-60" />
        <Link
          to="/tickets"
          className="rounded px-1.5 py-0.5 transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
        >
          Tickets
        </Link>
        <ChevronRight className="h-3 w-3 opacity-60" />
        <span className="rounded px-1.5 py-0.5 text-[var(--color-foreground)]">
          {ticketNumber ?? "…"}
        </span>
      </nav>

      <Button variant="outline" size="sm" onClick={onBack} className="gap-1.5">
        <ArrowLeft className="h-3.5 w-3.5" />
        <span className="hidden sm:inline">Back to tickets</span>
      </Button>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Hero — status timeline + identity + actions
// ───────────────────────────────────────────────────────────────────────

function Hero({
  ticket,
  isFetching,
  onRefresh,
  onResolve,
  onReopen,
  onAssign,
}: {
  ticket: TicketDto;
  isFetching: boolean;
  onRefresh: () => void;
  onResolve: () => void;
  onReopen: () => void;
  onAssign: () => void;
}) {
  const tone = STATUS_TONE[ticket.status];
  const StatusIcon = STATUS_ICON[ticket.status];
  const canResolve = ticket.status !== "Resolved" && ticket.status !== "Closed";
  const canReopen = ticket.status === "Resolved" || ticket.status === "Closed";

  return (
    <section
      className={cn(
        "fsh-enter fsh-enter-2 card-shell relative overflow-hidden rounded-[20px]",
        "bg-[var(--color-surface-3)]",
      )}
    >
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage: `
            radial-gradient(70% 70% at 0% 0%, oklch(from ${STATUS_COLOR[tone]} l c h / 0.16), transparent 60%),
            radial-gradient(50% 60% at 100% 0%, oklch(from var(--color-primary) l c h / 0.06), transparent 65%),
            radial-gradient(80% 80% at 100% 100%, oklch(from ${STATUS_COLOR[tone]} l c h / 0.04), transparent 70%)
          `,
        }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.05] mix-blend-overlay"
        style={{
          backgroundImage:
            "url(\"data:image/svg+xml;utf8,<svg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='2' stitchTiles='stitch'/></filter><rect width='100%' height='100%' filter='url(%23n)'/></svg>\")",
        }}
      />

      <div className="relative p-6 md:p-8 lg:p-10">
        {/* Eyebrow + actions */}
        <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-x-2.5 gap-y-1">
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              Helpdesk · Item
            </span>
            <span aria-hidden className="h-px w-6 bg-[var(--color-border-strong)]" />
            <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px] font-medium tracking-tight text-[var(--color-foreground)]">
              {ticket.number}
            </code>
          </div>
          <div className="flex items-center gap-1.5">
            <Button
              variant="outline"
              size="sm"
              disabled={isFetching}
              onClick={onRefresh}
              className="gap-1.5"
            >
              <RefreshCw className={cn("h-3.5 w-3.5", isFetching && "animate-spin")} />
              <span className="hidden sm:inline">Refresh</span>
            </Button>
            <Button variant="outline" size="sm" onClick={onAssign} className="gap-1.5">
              {ticket.assignedToUserId ? (
                <UserCheck className="h-3.5 w-3.5" />
              ) : (
                <UserX className="h-3.5 w-3.5" />
              )}
              <span className="hidden sm:inline">
                {ticket.assignedToUserId ? "Reassign" : "Assign"}
              </span>
            </Button>
            {canResolve && (
              <Button
                onClick={onResolve}
                size="sm"
                className="brand-glow gradient-sheen gap-1.5"
              >
                <CheckCircle2 className="h-3.5 w-3.5" />
                Resolve
              </Button>
            )}
            {canReopen && (
              <Button onClick={onReopen} size="sm" variant="outline" className="gap-1.5">
                <RotateCcw className="h-3.5 w-3.5" />
                Reopen
              </Button>
            )}
          </div>
        </div>

        {/* Title + priority */}
        <div className="mb-7">
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-display text-[32px] font-semibold leading-[1.05] tracking-[-0.02em] sm:text-[40px]">
              {ticket.title}
            </h1>
            <PriorityBadge priority={ticket.priority} />
          </div>
        </div>

        {/* Status timeline — the centerpiece. Visualizes the lifecycle as
            a horizontal track with dots, with the current status lit up. */}
        <StatusTimeline status={ticket.status} icon={StatusIcon} />
      </div>
    </section>
  );
}

function StatusTimeline({
  status,
  icon: ActiveIcon,
}: {
  status: TicketStatus;
  icon: React.ComponentType<{ className?: string }>;
}) {
  const order: TicketStatus[] = ["Open", "InProgress", "Resolved", "Closed"];
  const activeIdx = order.indexOf(status);

  return (
    <div className="flex flex-wrap items-center gap-x-1 gap-y-3">
      {order.map((s, i) => {
        const tone = STATUS_TONE[s];
        const isActive = i === activeIdx;
        const isPast = i < activeIdx;
        const Icon = isActive ? ActiveIcon : STATUS_ICON[s];

        return (
          <div key={s} className="flex items-center gap-1">
            <span
              aria-current={isActive ? "step" : undefined}
              className={cn(
                "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1",
                "font-mono text-[10.5px] font-medium uppercase tracking-[0.16em]",
                "transition-colors duration-[var(--duration-default)]",
              )}
              style={{
                backgroundColor: isActive
                  ? `oklch(from ${STATUS_COLOR[tone]} l c h / 0.16)`
                  : "var(--color-muted)",
                color: isActive
                  ? STATUS_COLOR[tone]
                  : isPast
                    ? "var(--color-foreground)"
                    : "var(--color-muted-foreground)",
                boxShadow: isActive
                  ? `inset 0 0 0 1px oklch(from ${STATUS_COLOR[tone]} l c h / 0.30)`
                  : undefined,
              }}
            >
              <Icon className="h-3 w-3" />
              {STATUS_LABEL[s]}
            </span>
            {i < order.length - 1 && (
              <span
                aria-hidden
                className="h-px w-4 sm:w-6"
                style={{
                  backgroundColor: isPast || (isActive && i < activeIdx)
                    ? "var(--color-border-strong)"
                    : "var(--color-border)",
                }}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}

function PriorityBadge({ priority }: { priority: TicketPriority }) {
  const map: Record<TicketPriority, { color: string }> = {
    Low: { color: "var(--color-muted-foreground)" },
    Medium: { color: "oklch(0.700 0.155 195)" },
    High: { color: "var(--color-warning)" },
    Critical: { color: "var(--color-destructive)" },
  };
  const m = map[priority];
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full px-2 py-0.5",
        "font-mono text-[10px] font-medium uppercase tracking-[0.16em]",
      )}
      style={{
        backgroundColor: `oklch(from ${m.color} l c h / 0.10)`,
        color: m.color,
      }}
    >
      {priority === "Critical" && <AlertOctagon className="h-3 w-3" />}
      {priority === "High" && <AlertTriangle className="h-3 w-3" />}
      {PRIORITY_LABEL[priority]}
    </span>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Description panel
// ───────────────────────────────────────────────────────────────────────

function DescriptionPanel({ ticket }: { ticket: TicketDto }) {
  return (
    <section
      className={cn(
        "fsh-enter fsh-enter-3 card-shell rounded-2xl",
        "bg-[var(--color-surface-3)] p-6 md:p-7",
      )}
    >
      <div className="flex items-baseline gap-2.5">
        <h2 className="text-display text-[15px] font-semibold tracking-[-0.01em]">
          Description
        </h2>
        <span className="font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
          original report
        </span>
      </div>
      <div className="mt-3">
        {ticket.description ? (
          <p className="whitespace-pre-wrap text-[14px] leading-relaxed text-[var(--color-foreground)]/90">
            {ticket.description}
          </p>
        ) : (
          <p className="italic text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
            No description on file. Comments below carry the conversation.
          </p>
        )}
      </div>

      {ticket.resolutionNote && (
        <div
          className={cn(
            "mt-5 rounded-xl border px-4 py-3",
            "border-[oklch(from_var(--color-success)_l_c_h_/_0.30)]",
            "bg-[oklch(from_var(--color-success)_l_c_h_/_0.06)]",
          )}
        >
          <div className="mb-1 flex items-center gap-1.5 font-mono text-[10px] uppercase tracking-[0.18em] text-[var(--color-success)]">
            <CheckCircle2 className="h-3 w-3" />
            Resolution
          </div>
          <p className="whitespace-pre-wrap text-[13.5px] leading-relaxed text-[var(--color-foreground)]/90">
            {ticket.resolutionNote}
          </p>
        </div>
      )}
    </section>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Comments thread
// ───────────────────────────────────────────────────────────────────────

function CommentsPanel({
  ticketId,
  comments,
  isLoading,
  onPosted,
  disabled,
}: {
  ticketId: string;
  comments: { id: string; authorUserId: string; body: string; createdAtUtc: string }[];
  isLoading: boolean;
  onPosted: () => void;
  disabled: boolean;
}) {
  const [body, setBody] = useState("");

  const mutation = useMutation({
    mutationFn: () => addTicketComment(ticketId, body.trim()),
    onSuccess: () => {
      setBody("");
      toast.success("Comment posted");
      onPosted();
    },
    onError: (e) => toast.error(describe(e)),
  });

  return (
    <section
      className={cn(
        "fsh-enter fsh-enter-4 card-shell rounded-2xl",
        "bg-[var(--color-surface-3)] p-6 md:p-7",
      )}
    >
      <div className="flex items-baseline justify-between gap-2.5">
        <div className="flex items-baseline gap-2.5">
          <h2 className="text-display text-[15px] font-semibold tracking-[-0.01em]">
            Conversation
          </h2>
          <span className="font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
            {comments.length === 0 ? "no comments yet" : `${comments.length} comment${comments.length === 1 ? "" : "s"}`}
          </span>
        </div>
        <MessageCircle className="h-4 w-4 text-[var(--color-muted-foreground)]" aria-hidden />
      </div>

      <div className="mt-5 space-y-4">
        {isLoading ? (
          <div className="space-y-3">
            <Skeleton className="h-12 w-full rounded-xl" />
            <Skeleton className="h-12 w-3/4 rounded-xl" />
          </div>
        ) : comments.length === 0 ? (
          <p className="italic text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
            No comments yet. Add the first note below — visible to anyone with View access.
          </p>
        ) : (
          <ul className="space-y-4">
            {comments.map((c) => (
              <CommentItem key={c.id} comment={c} />
            ))}
          </ul>
        )}
      </div>

      {/* Composer */}
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (!body.trim() || disabled) return;
          mutation.mutate();
        }}
        className={cn(
          "mt-6 rounded-xl border bg-[var(--color-surface-2)]",
          "border-[var(--color-border)] p-3",
          "focus-within:border-[var(--color-input)]",
        )}
      >
        <textarea
          value={body}
          onChange={(e) => setBody(e.target.value)}
          placeholder={
            disabled
              ? "Reopen the ticket to add a new comment."
              : "Add a comment to the thread…"
          }
          disabled={disabled}
          rows={3}
          className={cn(
            "block w-full bg-transparent text-sm leading-relaxed",
            "placeholder:text-[var(--color-muted-foreground)]",
            "resize-none outline-none focus:outline-none focus-visible:outline-none",
          )}
          maxLength={8192}
        />
        <div className="mt-2 flex items-center justify-between gap-2">
          <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            {body.length} / 8192
          </span>
          <Button
            type="submit"
            size="sm"
            disabled={!body.trim() || disabled || mutation.isPending}
            className="brand-glow gradient-sheen gap-1.5"
          >
            <Send className="h-3.5 w-3.5" />
            {mutation.isPending ? "Posting…" : "Post comment"}
          </Button>
        </div>
      </form>
    </section>
  );
}

function CommentItem({
  comment,
}: {
  comment: { authorUserId: string; body: string; createdAtUtc: string };
}) {
  const initial =
    comment.authorUserId.replace(/[^a-z0-9]/gi, "").charAt(0).toUpperCase() || "?";
  return (
    <li className="flex gap-3">
      <span
        aria-hidden
        className={cn(
          "grid h-8 w-8 shrink-0 place-items-center rounded-full",
          "bg-[var(--color-primary-soft)] text-[var(--color-primary)]",
          "font-mono text-[11px] font-semibold ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.30)]",
        )}
      >
        {initial}
      </span>
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
          <code
            title={comment.authorUserId}
            className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-foreground)]"
          >
            {comment.authorUserId.slice(0, 8)}…
          </code>
          <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            {formatRelative(comment.createdAtUtc)}
          </span>
        </div>
        <p className="mt-1 whitespace-pre-wrap text-[13.5px] leading-relaxed text-[var(--color-foreground)]/90">
          {comment.body}
        </p>
      </div>
    </li>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Metadata sidebar
// ───────────────────────────────────────────────────────────────────────

function MetadataPanel({ ticket }: { ticket: TicketDto }) {
  return (
    <aside
      className={cn(
        "fsh-enter fsh-enter-3 card-shell rounded-2xl",
        "bg-[var(--color-surface-3)] p-6",
      )}
    >
      <div className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
        Audit
      </div>
      <dl className="mt-3 space-y-3 text-[13px]">
        <Meta label="Reporter">
          <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px]">
            {ticket.reporterUserId.slice(0, 8)}…
          </code>
        </Meta>
        <Meta label="Assignee">
          {ticket.assignedToUserId ? (
            <code className="rounded bg-[var(--color-primary-soft)] px-1.5 py-0.5 font-mono text-[11px] text-[var(--color-primary)]">
              {ticket.assignedToUserId.slice(0, 8)}…
            </code>
          ) : (
            <span className="font-mono text-[11px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
              unassigned
            </span>
          )}
        </Meta>
        <Meta label="Created" value={formatDateMono(ticket.createdAtUtc)} hint={formatRelative(ticket.createdAtUtc)} />
        {ticket.updatedAtUtc && (
          <Meta label="Updated" value={formatDateMono(ticket.updatedAtUtc)} hint={formatRelative(ticket.updatedAtUtc)} />
        )}
        {ticket.resolvedAtUtc && (
          <Meta label="Resolved" value={formatDate(ticket.resolvedAtUtc)} hint={formatRelative(ticket.resolvedAtUtc)} />
        )}
      </dl>
    </aside>
  );
}

function Meta({
  label,
  value,
  hint,
  children,
}: {
  label: string;
  value?: React.ReactNode;
  hint?: string;
  children?: React.ReactNode;
}) {
  return (
    <div>
      <dt className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
        {label}
      </dt>
      <dd className="mt-1 flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
        {children ?? <span className="font-mono tabular-nums">{value}</span>}
        {hint && (
          <span className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            {hint}
          </span>
        )}
      </dd>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Resolve dialog
// ───────────────────────────────────────────────────────────────────────

function ResolveDialog({
  open,
  ticket,
  onClose,
}: {
  open: boolean;
  ticket: TicketDto;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [note, setNote] = useState("");

  useEffect(() => {
    if (open) setNote("");
  }, [open]);

  const mutation = useMutation({
    mutationFn: () => resolveTicket(ticket.id, note.trim() || null),
    onSuccess: () => {
      toast.success("Ticket resolved");
      void queryClient.invalidateQueries({ queryKey: ["tickets"] });
      onClose();
    },
    onError: (e) => toast.error(describe(e)),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <DialogHeader>
          <div className="flex items-center gap-2">
            <span
              aria-hidden
              className="grid h-6 w-6 place-items-center rounded-md bg-[oklch(from_var(--color-success)_l_c_h_/_0.16)] text-[var(--color-success)]"
            >
              <CheckCircle2 className="h-3.5 w-3.5" />
            </span>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              Mark resolved
            </span>
          </div>
          <DialogTitle>{ticket.number} — {ticket.title}</DialogTitle>
          <DialogDescription>
            Resolution notes are kept on the ticket and shown in the description panel.
            Leave blank if there's nothing to add.
          </DialogDescription>
        </DialogHeader>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            mutation.mutate();
          }}
        >
          <DialogBody className="space-y-4">
            <Field id="resolve-note" label="Resolution note" hint="Optional — leave blank if there's nothing to add.">
              <textarea
                id="resolve-note"
                value={note}
                onChange={(e) => setNote(e.target.value)}
                placeholder="What changed? Root cause? Anything reviewers should know."
                rows={4}
                className={cn(
                  "block w-full rounded-md border border-[var(--color-input)] bg-[var(--color-surface-2)]",
                  "px-3 py-2 text-sm shadow-[var(--shadow-xs)] placeholder:text-[var(--color-muted-foreground)]",
                  "focus-visible:border-[var(--color-input)]",
                )}
                maxLength={4096}
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">Cancel</Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending}
              className="brand-glow gradient-sheen gap-1.5"
            >
              <CheckCircle2 className="h-3.5 w-3.5" />
              {mutation.isPending ? "Resolving…" : "Mark resolved"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Assign dialog
// ───────────────────────────────────────────────────────────────────────

function AssignDialog({
  open,
  ticket,
  onClose,
}: {
  open: boolean;
  ticket: TicketDto;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [assignee, setAssignee] = useState(ticket.assignedToUserId ?? "");
  const [touched, setTouched] = useState(false);

  useEffect(() => {
    if (open) {
      setAssignee(ticket.assignedToUserId ?? "");
      setTouched(false);
    }
  }, [open, ticket.assignedToUserId]);

  const mutation = useMutation({
    mutationFn: () => assignTicket(ticket.id, assignee.trim() || null),
    onSuccess: () => {
      toast.success(assignee.trim() ? "Ticket assigned" : "Ticket unassigned");
      void queryClient.invalidateQueries({ queryKey: ["tickets"] });
      onClose();
    },
    onError: (e) => toast.error(describe(e)),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setTouched(true);
    mutation.mutate();
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
              <UserCheck className="h-3.5 w-3.5" />
            </span>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              {ticket.assignedToUserId ? "Reassign" : "Assign"}
            </span>
          </div>
          <DialogTitle>{ticket.number} — {ticket.title}</DialogTitle>
          <DialogDescription>
            Paste a user ID. Picking up the ticket transitions it to{" "}
            <code className="font-mono">In progress</code>; clearing the assignee on an
            in-progress ticket sends it back to <code className="font-mono">Open</code>.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={onSubmit}>
          <DialogBody className="space-y-4">
            <Field
              id="assignee"
              label="Assignee"
              hint="Search by name or email. Clear the selection to unassign."
            >
              <UserPicker
                value={assignee || null}
                onChange={(userId) => {
                  setAssignee(userId ?? "");
                  setTouched(true);
                }}
              />
            </Field>
            {touched && assignee.trim() === "" && (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                Clearing the assignee will <span className="font-medium text-[var(--color-foreground)]">unassign</span> the ticket.
              </p>
            )}
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">Cancel</Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending}
              className="brand-glow gradient-sheen gap-1.5"
            >
              <Sparkles className="h-3.5 w-3.5" />
              {mutation.isPending ? "Saving…" : "Save assignment"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Skeleton + NotFound
// ───────────────────────────────────────────────────────────────────────

function DetailSkeleton() {
  return (
    <>
      <div className="card-shell rounded-[20px] bg-[var(--color-surface-3)] p-6 md:p-8 lg:p-10">
        <div className="space-y-5">
          <Skeleton className="h-3 w-32" />
          <Skeleton className="h-10 w-3/4" />
          <Skeleton className="h-7 w-2/3" />
        </div>
      </div>
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[1fr_320px]">
        <div className="space-y-5">
          <Skeleton className="h-32 rounded-2xl" />
          <Skeleton className="h-64 rounded-2xl" />
        </div>
        <Skeleton className="h-64 rounded-2xl" />
      </div>
    </>
  );
}

function NotFoundPanel() {
  return (
    <div
      className={cn(
        "card-shell rounded-2xl bg-[var(--color-surface-3)]",
        "flex flex-col items-center gap-4 px-8 py-16 text-center",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "grid h-14 w-14 place-items-center rounded-2xl",
          "bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.18),oklch(from_var(--color-primary)_l_c_h_/_0.02))]",
          "ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.22)]",
        )}
      >
        <TicketIcon className="h-6 w-6 text-[var(--color-primary)]" />
      </span>
      <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
        Not found
      </span>
      <h3 className="text-display text-xl font-semibold tracking-[-0.02em]">
        This ticket no longer exists.
      </h3>
      <p className="max-w-md text-sm leading-relaxed text-[var(--color-muted-foreground)]">
        It may have been deleted. Check the trash, or head back to the tickets desk.
      </p>
      <Link to="/tickets">
        <Button>Back to tickets</Button>
      </Link>
    </div>
  );
}

// Reserved for future "ChevronRight"-style suffix in the breadcrumb on
// narrower viewports — suppress unused-import warnings if added later.
void TICKET_PRIORITIES;

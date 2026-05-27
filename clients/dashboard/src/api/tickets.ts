import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

// ─── Enums (string-serialized via JsonStringEnumConverter) ───────────────

export type TicketStatus = "Open" | "InProgress" | "Resolved" | "Closed";

export type TicketPriority = "Low" | "Medium" | "High" | "Critical";

export const TICKET_STATUSES: readonly TicketStatus[] = [
  "Open",
  "InProgress",
  "Resolved",
  "Closed",
] as const;

export const TICKET_PRIORITIES: readonly TicketPriority[] = [
  "Low",
  "Medium",
  "High",
  "Critical",
] as const;

// ─── DTOs ─────────────────────────────────────────────────────────────────

export type TicketDto = {
  id: string;
  number: string;
  title: string;
  description?: string | null;
  status: TicketStatus;
  priority: TicketPriority;
  reporterUserId: string;
  assignedToUserId?: string | null;
  resolutionNote?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  resolvedAtUtc?: string | null;
  closedAtUtc?: string | null;
  commentCount: number;
  deletedOnUtc?: string | null;
  deletedBy?: string | null;
};

export type TicketCommentDto = {
  id: string;
  ticketId: string;
  authorUserId: string;
  body: string;
  createdAtUtc: string;
};

// ─── Inputs ───────────────────────────────────────────────────────────────

export type SearchTicketsParams = {
  search?: string;
  status?: TicketStatus | null;
  priority?: TicketPriority | null;
  assignedToUserId?: string | null;
  reporterUserId?: string | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateTicketInput = {
  title: string;
  description?: string | null;
  priority?: TicketPriority;
  assignedToUserId?: string | null;
};

// ─── Endpoints ────────────────────────────────────────────────────────────

const BASE = "/api/v1";

export function searchTickets(
  params: SearchTicketsParams = {},
): Promise<PagedResponse<TicketDto>> {
  const q = new URLSearchParams();
  if (params.search) q.set("search", params.search);
  if (params.status) q.set("status", params.status);
  if (params.priority) q.set("priority", params.priority);
  if (params.assignedToUserId) q.set("assignedToUserId", params.assignedToUserId);
  if (params.reporterUserId) q.set("reporterUserId", params.reporterUserId);
  q.set("pageNumber", String(params.pageNumber ?? 1));
  q.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) q.set("sortBy", params.sortBy);
  if (params.sortDir) q.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<TicketDto>>(`${BASE}/tickets?${q.toString()}`);
}

export function getTicketById(id: string): Promise<TicketDto> {
  return apiFetch<TicketDto>(`${BASE}/tickets/${encodeURIComponent(id)}`);
}

export function createTicket(input: CreateTicketInput): Promise<string> {
  return apiFetch<string>(`${BASE}/tickets`, {
    method: "POST",
    body: JSON.stringify({
      title: input.title,
      description: input.description ?? null,
      priority: input.priority ?? "Medium",
      assignedToUserId: input.assignedToUserId ?? null,
    }),
  });
}

export function assignTicket(ticketId: string, assigneeUserId: string | null): Promise<string> {
  return apiFetch<string>(`${BASE}/tickets/${encodeURIComponent(ticketId)}/assign`, {
    method: "POST",
    body: JSON.stringify({ assigneeUserId }),
  });
}

export function resolveTicket(ticketId: string, resolutionNote?: string | null): Promise<string> {
  return apiFetch<string>(`${BASE}/tickets/${encodeURIComponent(ticketId)}/resolve`, {
    method: "POST",
    body: JSON.stringify({ resolutionNote: resolutionNote ?? null }),
  });
}

export function reopenTicket(ticketId: string): Promise<string> {
  return apiFetch<string>(`${BASE}/tickets/${encodeURIComponent(ticketId)}/reopen`, {
    method: "POST",
  });
}

export function addTicketComment(ticketId: string, body: string): Promise<string> {
  return apiFetch<string>(`${BASE}/tickets/${encodeURIComponent(ticketId)}/comments`, {
    method: "POST",
    body: JSON.stringify({ body }),
  });
}

export function listTicketComments(ticketId: string): Promise<TicketCommentDto[]> {
  return apiFetch<TicketCommentDto[]>(
    `${BASE}/tickets/${encodeURIComponent(ticketId)}/comments`,
  );
}

// ─── Trash + Restore (each top-level resource exposes the same shape) ─────

export function listTrashedTickets(
  pageNumber = 1,
  pageSize = 20,
): Promise<PagedResponse<TicketDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<TicketDto>>(`${BASE}/tickets/trash?${q.toString()}`);
}

export function restoreTicket(ticketId: string): Promise<string> {
  return apiFetch<string>(`${BASE}/tickets/${encodeURIComponent(ticketId)}/restore`, {
    method: "POST",
  });
}

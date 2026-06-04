import type { TicketPriority, TicketStatus } from "@/api/tickets";
import type { EntityStatusTone } from "@/components/list";

// Shared label + tone maps for ticket status/priority, used by the tickets list and
// the ticket detail page so the two never drift.

export const STATUS_LABEL: Record<TicketStatus, string> = {
  Open: "Open",
  InProgress: "In progress",
  Resolved: "Resolved",
  Closed: "Closed",
};

export const STATUS_TONE: Record<TicketStatus, EntityStatusTone> = {
  Open: "info",
  InProgress: "warning",
  Resolved: "success",
  Closed: "default",
};

export const PRIORITY_LABEL: Record<TicketPriority, string> = {
  Low: "Low",
  Medium: "Medium",
  High: "High",
  Critical: "Critical",
};

export const PRIORITY_TONE: Record<TicketPriority, EntityStatusTone> = {
  Low: "default",
  Medium: "info",
  High: "warning",
  Critical: "danger",
};

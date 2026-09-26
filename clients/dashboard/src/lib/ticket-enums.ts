import type { TicketPriority, TicketStatus } from "@/api/tickets";
import type { EntityStatusTone } from "@/components/list";

// Shared tone maps + i18n key maps for ticket status/priority, used by the tickets list
// and the ticket detail page so the two never drift. Labels are resolved at each call
// site via t(STATUS_LABEL_KEY[status]) against the "tickets" namespace.

export const STATUS_LABEL_KEY: Record<TicketStatus, string> = {
  Open: "status.Open",
  InProgress: "status.InProgress",
  Resolved: "status.Resolved",
  Closed: "status.Closed",
};

export const STATUS_TONE: Record<TicketStatus, EntityStatusTone> = {
  Open: "info",
  InProgress: "warning",
  Resolved: "success",
  Closed: "default",
};

export const PRIORITY_LABEL_KEY: Record<TicketPriority, string> = {
  Low: "priority.Low",
  Medium: "priority.Medium",
  High: "priority.High",
  Critical: "priority.Critical",
};

export const PRIORITY_TONE: Record<TicketPriority, EntityStatusTone> = {
  Low: "default",
  Medium: "info",
  High: "warning",
  Critical: "danger",
};

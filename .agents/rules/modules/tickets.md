# Module: Tickets

Support ticket lifecycle with comments. Module `Order = 700`.

**Entities / DbContext:** `Ticket` (aggregate, soft-deletable, state machine) + `TicketComment`. `TicketsDbContext`. `TicketStatus`/`TicketPriority` enums in Contracts; domain events internal.
**Areas:** Create, Assign, Resolve, Reopen, Restore, AddComment, ListComments, GetById, Search, ListTrashed. Full list: `Features/v1/` or `/scalar`.

## Gotchas

- **State machine** (`Domain/Ticket.cs`): `Open → InProgress → Resolved → Closed`. Illegal transitions throw **`CustomException` with `HttpStatusCode.Conflict` (409)** — not a generic 400. Assigning auto-starts (Open→InProgress); unassigning an InProgress ticket reverts to Open; creating with an assignee starts at InProgress. Closed tickets reject comments/resolve until reopened. Keep all transition guards in the aggregate.
- Soft-delete/restore/trash pattern is identical to Catalog (filtered unique indexes — see `modules/catalog.md`).
- Endpoints are mapped on the bare `api/v{version}` group (no `/tickets` sub-path); literal routes precede `{ticketId:guid}`.

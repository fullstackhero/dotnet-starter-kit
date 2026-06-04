---
description: Review the current diff against FSH conventions and emit a structured report. Run after code changes, before commit. Read-only review (do not fix unless asked).
---

You review code changes for FullStackHero against its conventions and output a structured report. The
conventions are defined in `.agents/rules/` and `AGENTS.md` — treat those as the source of truth; this
playbook is the review procedure, not a second copy of the rules.

## Procedure
1. `git diff HEAD` (and `git status`) to see what changed; group by area (backend module / BuildingBlocks / frontend).
2. For each changed file, check it against the relevant rule file (`api-conventions.md`, `database.md`, `eventing.md`, `frontend/*`, …) and the checklist below.
3. If the Roslyn navigator MCP is available, run `detect_antipatterns` and `get_diagnostics` (solution scope) for machine-found issues (broad `catch`, missing `CancellationToken`, EF `AsNoTracking`, logging interpolation) and fold them in — noting false positives (mutate-then-save queries don't want `AsNoTracking`; hosted-service `catch(Exception)` that logs + filters OCE is fine).
4. Report with `file:line` refs and a concrete fix per finding.

## Checklist (high-signal)
**Boundaries / structure**
- Cross-module references go only through `.Contracts` (never another module's runtime). Enforced by `Architecture.Tests`.
- `src/BuildingBlocks/**` not modified without explicit approval (flag if it is).
- New module → registered in **all four** places (Mediator + `moduleAssemblies` in Api **and** DbMigrator).

**CQRS / Mediator (not MediatR)**
- Command/Query in the Contracts project; `using Mediator;` (`ICommand<T>`/`IQuery<T>`).
- Handler `public sealed`, `ICommandHandler<,>`/`IQueryHandler<,>`, returns `ValueTask<T>`, `.ConfigureAwait(false)`, injects the `{X}DbContext` (no generic repository).
- Every command + paginated query has a `{Name}Validator` (Architecture.Tests enforces).

**Endpoints**
- `internal static …Map{Feature}Endpoint`; `.RequirePermission(...)` (or deliberate `.AllowAnonymous()`); `.WithName`/`.WithSummary`. Returns `Results.Ok(...)`/`TypedResults`. `.WithIdempotency()` on replay-safe POSTs. No duplicate `IRequiredPermissionMetadata`.

**Data**
- Entities: `sealed`, `Guid.CreateVersion7()`, private ctor + factory, behavior via methods. Marker interfaces use `CreatedOnUtc`/`IsDeleted`/`DeletedOnUtc`.
- DbContext extends `BaseDbContext`, `base.OnModelCreating` last; **no manual tenant/soft-delete query filter**. Nav-collection children need `ValueGeneratedNever()`. `AsNoTracking` on read-only queries only (not read-then-save).

**Cross-cutting**
- **Structured logging only** — no `$"..."` interpolation in log calls.
- `CancellationToken` propagated into EF/IO calls.
- Cross-module events go via the Outbox (`IOutboxStore.AddAsync`), not a direct bus publish.

**Frontend** (`frontend/*` rules)
- Hand-written types + `apiFetch`; mutation data passed via `mutate(arg)`; query keys hierarchical; admin gates routes with `RouteGuard` + mirrors the permission; dashboard uses `withSuspense`.

## Commands
```bash
git diff HEAD
grep -rn "MediatR\|IRequest<\|IRequestHandler<" src/Modules/ --include="*.cs"   # must be empty
dotnet build src/FSH.Starter.slnx 2>&1 | grep -E "warning|error"               # 0 expected
```

## Output
```
## Code Review

### Passed
- …

### Violations (file:line)
1. {rule} — {file}:{line}
   Issue: …
   Fix: …

### Warnings / suggestions
- …

### Verification
dotnet build src/FSH.Starter.slnx   → expect 0 warnings
dotnet test src/FSH.Starter.slnx    (integration tests need Docker)
```

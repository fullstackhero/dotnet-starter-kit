---
description: Orchestrate delivering a feature end-to-end. Sequences the scaffolding skills and verifies. Use when asked to "add a feature/endpoint/screen". Delegates the code recipes to skills — does not restate them.
---

You orchestrate feature delivery for FullStackHero. **You do not duplicate code templates** — each phase
invokes the canonical skill, which holds the current, verified recipe. Your job is sequencing, the
backend↔frontend contract, and verification.

## Clarify first
1. Module (existing? if not → `module-creator`).
2. Operation: command (state change) or query (read)?
3. Does it need a new entity? (→ Phase 0)
4. UI surface: backend-only, `admin`, or `dashboard`?
5. Request fields + response shape + permission.

## Phases (delegate each recipe to its skill)
- **Phase 0 — entity (if new):** follow the **`add-entity`** skill, then **`create-migration`**.
- **Phase 1 — backend slice:** follow the **`add-feature`** skill (Command/Query in Contracts → handler injecting the `{X}DbContext` → validator → endpoint → wire in `MapEndpoints`). Add a handler/validator test per **`testing-guide`**. Build + test green before moving on.
- **Phase 2 — frontend (if a UI surface):** lock the contract (route, request shape, **response DTO field names — JSON is camelCase**), then follow the **`add-react-page`** skill for the chosen app. For the whole flow at once, use the **`add-full-slice`** skill.
- **Phase 3 — permission (if gated):** follow the **`add-permission`** skill (server constant + admin mirror/guard).

## Verify
```bash
dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/{X}.Tests
# if a UI surface: cd clients/{app} && npm run lint && npm run test:e2e
```
Then run the **`code-reviewer`** and **`architecture-guard`** workflows before commit.

## Guardrails (the skills enforce these; confirm them)
- CQRS types live in the **Contracts** project; handlers are `public sealed`, return `ValueTask<T>`, `.ConfigureAwait(false)`.
- Every command + paginated query has a `{Name}Validator` (Architecture.Tests fails otherwise).
- Endpoints gated with `.RequirePermission(...)`; structured logging only; `CancellationToken` propagated.

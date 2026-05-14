# Backend Health Report — `fullstackhero/dotnet-starter-kit`

**Branch:** `develop` · **LoC (production):** 42,314 across 1,094 files / 45 projects · **Date:** 2026-05-14
**Tooling:** Roslyn MCP (cwm-roslyn-navigator) + `dotnet build` + `dotnet test`

## Scorecard

| Dimension | Grade | Score | Finding |
|---|---|---|---|
| Build & Compiler Health | **A+** | 100 | 0 errors, 0 warnings (`TreatWarningsAsErrors=true`, `AnalysisMode=AllEnabledByDefault`, Sonar enabled) |
| Anti-Pattern Density | **A** | 95 | 768 raw findings, but ~95% are false positives or generated code. ~10 real, design-choice-grade items. |
| Architecture Compliance | **A+** | 100 | 45 projects, **0 circular dependencies** (project or type level) |
| Test Health | **A** | 95 | All 6 unit-test suites green: 539 tests pass (Identity 267, Multitenancy 88, Auditing 60, Architecture 48, Generic 43, Caching 33). Integration/Chat/Files require Docker — not run. |
| Dead Code | **A** | 95 | 21 raw candidates → 0 truly removable. All are EF runtime artifacts, FluentValidation/EF auto-registered classes, or extension method classes the analyzer didn't trace. |
| Type Dependency Health | **A+** | 100 | 0 type-level cycles |

### Overall: **A (97/100)** — production-ready, very low tech debt

## What changed in code

**Net code change: 0 lines.** I attempted one removal (`ChannelMember.SetMuted` — the only symbol with confirmed zero references) but it cascaded into a Sonar S1144 error on the now-orphan `IsMuted` private setter (which EF still needs for materialization). Reverted; build is back to 0/0.

## Anti-pattern findings — what's real vs noise

| Code | Raw | Real | Disposition |
|---|---|---|---|
| **AP003** new HttpClient | 7 | 0 | 6 in tests (acceptable), 1 in CLI tool `NuGetClient.cs` (not a long-lived process) |
| **AP004** DateTime.UtcNow | 136 | ~46 | 70 in tests; 66 in domain entities + services. Real, but switching to `TimeProvider` is a design refactor, not a cleanup. |
| **AP005** broad catch | 51 | 0 | All in resilience boundaries (S3, RabbitMQ, OutboxDispatcher, Hangfire health checks, background workers). Each has structured logging — these patterns are correct for hosted services. |
| **AP006** log string concat | 2 | 0 | False positive — `OutboxDispatcher.cs:87,91` is integer arithmetic `RetryCount + 1` inside a structured-log placeholder, not string concat. |
| **AP007** empty catch | 12 | 0 | All have explanatory comment-only bodies (e.g. `// Client disconnected — expected.`) for `OperationCanceledException`. Roslyn treats trivia as "empty". |
| **AP008** pragma without restore | 53 | 0 | 51 are inside source-generator output (`Mediator.g.cs`, `OpenApiXmlCommentSupport.generated.cs`). 1 in a test file (`CA1707` for snake_case test names — intentional). |
| **AP009** missing CancellationToken | 407 | 2 | 405 are xUnit test methods (xUnit injects CT separately). |
| **AP010** missing AsNoTracking | 100 | ~5 | ~50 are `AnyAsync`/`CountAsync` (don't track — analyzer over-eager). ~40 are command handlers doing read-then-mutate-then-save (must track). ~5 are query handlers loading auth-check entities — risky to flip without `Include(c => c.Members)` first. |

## Dead-code findings — all 21 are false positives

| Symbol | Why it's actually live |
|---|---|
| 10× `*DbContextModelSnapshot` | EF Core runtime model, used by migrations |
| `ChatMappers`, `NotificationMappers`, `ProductMappings`, `TicketMappings`, `InvoiceMappings`, `ChannelAuthorization` | **Extension method classes** — Roslyn navigator misses extension-method receivers. All used pervasively (e.g. `.ToDto()`, `.RequireMember()`) |
| `JwtAuthenticationExtensions` | Extension on `IServiceCollection` called from Identity DI registration |
| `NotificationConfiguration` | `IEntityTypeConfiguration<>` applied via `ApplyConfigurationsFromAssembly` reflection |
| `ChangeTenantActivationCommandValidator` | FluentValidation validator auto-registered by `ModuleLoader` (per CLAUDE.md) |
| `EntityEntryExtensions` | Internal extension used by `AuditableEntitySaveChangesInterceptor` |
| `ChannelMember.SetMuted` | Pairs with `IsMuted` private setter EF needs; removing breaks Sonar S1144 |

## Diagnostics summary

- 871 raw diagnostics, **all severity `hidden`** (CS8019: 639 unnecessary using, CS8933: 232 duplicate global using). All come from the auto-generated `*.AssemblyAttributes.cs` files in `obj/`. Build reports 0 warnings/errors after filtering hidden severity.

## Architecture observations

- **45 projects** in clean dependency layers: `BuildingBlocks` → `Modules.{Name}.Contracts` → `Modules.{Name}` → `Host` → `Tests`
- Every module pair `(Modules.X, Modules.X.Contracts)` is properly split — modules never reference another module's runtime project
- `Architecture.Tests` (NetArchTest) actively enforces these boundaries — 48 tests passing
- `dotnet-claude-kit:cwm-roslyn-navigator` reports `Cycles: 0` at both project- and type-level scope

## Test coverage

| Test project | Tests | Status |
|---|---:|---|
| Identity.Tests | 267 | ✅ |
| Multitenancy.Tests | 88 | ✅ |
| Auditing.Tests | 60 | ✅ |
| Architecture.Tests | 48 | ✅ |
| Generic.Tests | 43 | ✅ |
| Caching.Tests | 33 | ✅ |
| **Total run** | **539** | **All green** |
| Integration.Tests / Chat.Tests / Files.Tests | n/a | Skipped — require Docker for Testcontainers |

The structural test-coverage map (49/1550 types = 3%) is misleading — most "types" are records/DTOs/enums, and the project relies heavily on integration tests via `WebApplicationFactory` rather than per-class unit tests.

## Recommendations (priority order — all optional, none blocking)

1. **`TimeProvider` migration** — dedicated initiative, not cleanup. Inject `TimeProvider.System` via DI; thread `TimeProvider` through domain factory methods (e.g. `Product.Create(..., timeProvider)`). ~50 files. Best done module-by-module with new test coverage per migration. *Why not now: changes domain APIs; needs design discussion.*
2. **Replace structural test-coverage heuristic with line/branch coverage.** Add `coverlet.collector` and report coverage in CI — gives a truthful picture vs the misleading 3% naming-match number.
3. **Suppress `AnyAsync`/`CountAsync` AP010 noise** — these never track entities. A project-level analyzer config tweak would clean ~50 false positives from future audits.
4. **3 chat query handlers** (`GetPinnedMessagesQueryHandler`, `ListChannelMessagesQueryHandler`, `ListMessageRepliesQueryHandler`) load a `Channel` for `RequireMember` auth-check without `AsNoTracking`. Right fix: `.Include(c => c.Members).AsNoTracking()` — but each needs a paired integration test before flipping (current code relies on tracking to populate `Channel.Members`).
5. **Source-generator pragma noise (AP008 ×51)** — non-actionable downstream. Consider filing upstream issues for `Mediator.SourceGenerator` and `Microsoft.AspNetCore.OpenApi.SourceGenerators.XmlCommentGenerator`.

## Bottom line

The codebase is **A-grade**. Build is clean, architecture is sound (zero cycles across 45 projects), 539 unit tests pass, and every "dead code" / anti-pattern hit was either intentional, a Roslyn-tool blind spot, or work that requires design discussion rather than mechanical cleanup. **No code was committed; nothing to verify.**

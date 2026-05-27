---
description: Verify changes don't violate architectural integrity — module boundaries, BuildingBlocks protection, the four-place module registration, and the architecture-test suite. Run before commit/PR. READ-ONLY.
---

You are the architecture guardian for FullStackHero. You verify integrity and report — **READ-ONLY, never
modify files.** The authoritative enforcement is `Architecture.Tests` (NetArchTest); the greps below are
fast heuristics that point you at things to confirm against the tests + `.agents/rules/architecture.md`.

## Steps

### 1. BuildingBlocks guard
```bash
git diff --name-only | grep -E "^src/BuildingBlocks/"
```
Any hit → **STOP and flag**: BuildingBlocks changes need explicit approval (wide blast radius).

### 2. Architecture tests (the real enforcement)
```bash
dotnet test src/Tests/Architecture.Tests
```
Covers: cross-module references only via `.Contracts`, tenant-isolation rules on entities, handlers `sealed`, and **every command/paginated-query handler has a validator**. All must pass.

### 3. Build clean
```bash
dotnet build src/FSH.Starter.slnx 2>&1 | grep -E "warning|error"   # expect none (TreatWarningsAsErrors)
```

### 4. Module boundary heuristic
```bash
grep -rn "using FSH.Modules\." src/Modules --include="*.cs" | grep -v "\.Contracts"
```
Cross-module `using`s should resolve only to `*.Contracts` namespaces (same-module internal usings are fine — confirm the module name differs).

### 5. Mediator, not MediatR
```bash
grep -rn "MediatR\|IRequest<\|IRequestHandler<" src/Modules --include="*.cs"   # must be empty
```

### 6. New-module registration (the four-place footgun)
If a new `*Module` was added, confirm it appears in **all four**: Mediator `o.Assemblies` (Contracts marker **and** module type) + `moduleAssemblies` array, in **both** `FSH.Starter.Api/Program.cs` and `FSH.Starter.DbMigrator/Program.cs`.
```bash
grep -rn "{New}Module\|{New}ContractsMarker" src/Host/FSH.Starter.Api/Program.cs src/Host/FSH.Starter.DbMigrator/Program.cs
```

### 7. Permission-gate integrity
Confirm exactly one `IRequiredPermissionMetadata` implementation exists — a duplicate silently disables **all** `.RequirePermission()` gates.
```bash
grep -rn "IRequiredPermissionMetadata" src --include="*.cs"
```

### 8. Tenant-isolation sanity
New module DbContexts extend `BaseDbContext` and call `base.OnModelCreating` **last**; opt-outs use `IGlobalEntity`. (Detailed rules: `database.md`, `modules/multitenancy.md`.)

## Output
```
## Architecture Verification

BuildingBlocks      : ✅ untouched | ⚠️ MODIFIED — needs approval
Architecture.Tests  : ✅ pass | ❌ {n} failed: {names}
Build               : ✅ 0 warnings | ❌ {n}
Module boundaries   : ✅ clean | ❌ {cross-module refs}
Mediator usage      : ✅ | ❌ MediatR detected at {file:line}
Module registration : ✅ 4/4 places | ❌ missing in {file}
Permission metadata : ✅ single | ❌ duplicate at {file:line}

Overall: ✅ PASS | ❌ FAIL — fix before commit
```

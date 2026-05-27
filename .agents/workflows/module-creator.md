---
description: Orchestrate bringing up a new module (bounded context) end-to-end and verifying it loads. Use when adding a new business domain. Delegates the recipe to the add-module skill — does not restate it.
---

You orchestrate a full module bring-up for FullStackHero. **The code recipe lives in the `add-module`
skill** — follow it; this playbook adds the decision gate, sequencing, and verification.

## Decide: is this really a new module?
A new module has its own domain entities and is a distinct bounded context. If it's just an operation in
an existing domain → use `feature-scaffolder` instead.

## Sequence (each step → its skill)
1. **Scaffold the module** — follow **`add-module`**: copy an existing module's two `.csproj` files; `[assembly: FshModule(typeof({X}Module), order)]` (assembly-level); `IModule` with `AddHeroDbContext<{X}DbContext>()`, `PermissionConstants.Register({X}Permissions.All)`, a version-set endpoint group, and the eventing trio if it publishes/handles events; `{X}DbContext : BaseDbContext` with `base.OnModelCreating` **last**.
2. **First entity** — follow **`add-entity`**.
3. **First feature** — follow **`add-feature`** (and `add-react-page` if it has UI).
4. **Migration** — follow **`create-migration`** with `--context {X}DbContext --output-dir {X}`; add the `{X}/` folder in the Migrations project.
5. **⚠️ Register in ALL FOUR places** — Mediator `o.Assemblies` (Contracts marker **and** module type) + `moduleAssemblies` array, in **both** `FSH.Starter.Api/Program.cs` **and** `FSH.Starter.DbMigrator/Program.cs`. Add to `.slnx`; reference the runtime project from Api, DbMigrator, and the Migrations project.

## Verify it actually loaded (not just compiled)
```bash
dotnet build src/FSH.Starter.slnx                 # 0 warnings
dotnet test src/Tests/Architecture.Tests          # boundary + tenant-isolation rules
dotnet run --project src/Host/FSH.Starter.DbMigrator -- list-pending   # new context shows up
```
Then hit one endpoint and confirm the handler runs — a missing Mediator marker compiles fine but the handler is silently undiscovered. Finish with the `architecture-guard` workflow.

## The footgun, restated
Four registration edits (2 lists × 2 host files). Miss the Mediator marker → handler not found at runtime. Miss the `moduleAssemblies` entry → module never loads. Miss the DbMigrator pair → migrate/seed skips it.

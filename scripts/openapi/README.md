# OpenAPI Client Generation

Use NSwag (local dotnet tool) to generate typed C# clients + DTOs from the FSH Starter API spec.

## Prereqs
- .NET SDK (repo already uses net10.0)
- Local tool manifest at `.config/dotnet-tools.json` (created) with `nswag.consolecore`

## One-liner
```powershell
./scripts/openapi/generate-api-clients.ps1 -SpecUrl "https://localhost:7030/openapi/v1.json"
```

This restores the local tool, ensures the output directory exists, and runs NSwag with the spec URL you provide.

## Output
- Clients + DTOs: Generated into the configured output path (single file; multiple client types grouped by first path segment after the base path, e.g., `/api/v1/identity/*` -> `IdentityClient`).
- Client grouping: `MultipleClientsFromPathSegments`; ensure Minimal API routes keep module-specific first segments.
- Bearer auth: configure `HttpClient` (via DI) with the bearer token; generated clients use injected `HttpClient`. Base URLs are not baked into the generated code (`useBaseUrl: false`), so `HttpClient.BaseAddress` must be set by the app.

## Drift Check (manual)
Use `./scripts/openapi/check-openapi-drift.ps1 -SpecUrl "<spec-url>"` to regenerate the clients and fail if `ApiClient/Generated.cs` changes. This is useful in PRs to ensure the spec and generated clients stay in sync even before CI enforcement.

> Note: The spec endpoint must be reachable when running the generation scripts. If the API is not running locally, point `-SpecUrl` to an accessible environment or start the FSH Starter API first.

## Tips
- If the API changes, rerun the script with the updated spec URL (e.g., staging/prod).
- Commit regenerated clients alongside related API changes to keep UI consumers in sync.

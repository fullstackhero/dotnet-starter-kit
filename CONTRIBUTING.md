# Contributing

Thanks for helping out. The conventions below keep PRs reviewable.

## Reporting issues

- **Security:** Use GitHub's private advisories — see [SECURITY.md](SECURITY.md). Do not file public issues for vulnerabilities.
- **Bugs:** Open a [GitHub issue](https://github.com/fullstackhero/dotnet-starter-kit/issues) with a minimal repro, your .NET SDK version, and the DB provider.
- **Features:** Start a [Discussion](https://github.com/fullstackhero/dotnet-starter-kit/discussions) before opening a PR for non-trivial work.

## Dev setup

Prerequisites: .NET 10 SDK, Docker, Node.js 22+.

```bash
dotnet build src/FSH.Starter.slnx
dotnet run --project src/Host/FSH.Starter.AppHost   # full Aspire stack
dotnet test src/FSH.Starter.slnx                    # tests (integration suite needs Docker)
```

Client apps live under `clients/admin` and `clients/dashboard` — `npm install && npm run dev` in each.

## Pull requests

- Branch from and target `main`.
- Follow [Conventional Commits](https://www.conventionalcommits.org) — match the existing history (`feat(chat): ...`, `fix(identity): ...`).
- Add tests. The build runs with `TreatWarningsAsErrors=true`; analyzer warnings must be fixed.
- Don't touch `src/BuildingBlocks/` without prior discussion — wide blast radius.
- Architecture rules (module boundaries, file layout, coding style) are documented in [CLAUDE.md](CLAUDE.md). Apply them.

## Code of conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md).

## Licensing

Contributions are licensed under the project's [MIT License](LICENSE).

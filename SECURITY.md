# Security Policy

## Supported versions

This is a starter kit. Only the current `main` branch receives security fixes from upstream. Forks, downstream projects, and tagged releases are owned by their maintainers — pull fixes in on your own cadence.

## Reporting a vulnerability

**Do not open a public issue.** Use GitHub's private vulnerability reporting:

<https://github.com/fullstackhero/dotnet-starter-kit/security/advisories/new>

Please include:

- Affected component (module, file, endpoint)
- Reproduction steps and any required configuration
- Impact (what an attacker can achieve)
- Proof-of-concept if you have one

## What to expect

- Acknowledgement within 72 hours.
- Triage decision within 7 days.
- Coordinated disclosure window of ~90 days from triage, longer for changes that need careful migration paths.

Fixes ship as a patched commit on `main` plus a GitHub Security Advisory. Reporters are credited with permission.

## Scope

In scope: `src/` (BuildingBlocks, Modules, Host), default `appsettings.*.json`, the `FullStackHero.CLI`, and the `clients/` apps.

Out of scope: third-party NuGet/npm packages (report upstream), the docs site, and issues in downstream forks (contact that fork's maintainer).

## Production hardening

This kit ships with development-friendly defaults. Before deploying a fork, rotate JWT signing keys and seeded passwords, lock CORS, set strong Hangfire dashboard credentials, and persist DataProtection keys to a shared store for multi-instance hosting.

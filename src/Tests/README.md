# Architecture Tests

This folder contains solution-wide architecture tests for the FullStackHero .NET 10 Starter Kit. The goal is to automatically enforce layering, dependency, and naming rules as the codebase evolves.

## Project

- `Architecture.Tests` targets `net10.0` and lives under `src/Tests/Architecture.Tests`.
- Test dependencies are limited to `xunit`, `Shouldly`, and `AutoFixture`, with versions defined in `src/Directory.Packages.props`.

## What Is Covered

- **Module dependencies**: module runtime projects (`Modules.*`) cannot reference other modules' runtime projects directly; only their own runtime, contracts, and building blocks are allowed (validated via csproj inspection).
- **Feature layering**: feature types under `Modules.*.Features.v{version}` are checked with NetArchTest to depend only on allowed layers (System/Microsoft, `FSH.Framework.*`, their module, and module contracts).
- **Host boundaries**: module code must not depend on host applications, and hosts must not depend directly on module feature or data internals.
- **Namespace conventions**: selected areas (for example, `BuildingBlocks/Core/Domain`) must declare namespaces that reflect the folder structure.

## Running the Tests

- Run all tests (including architecture tests): `dotnet test src/FSH.Starter.slnx`.
- Architecture tests are lightweight and rely only on project and file structure; they do not require any external services or databases.

## Extending the Rules

- Add new rules as additional test classes inside `Architecture.Tests`, following the existing patterns (using NetArchTest for type-level rules and reflection or project file inspection where appropriate).
- Keep rules fast and deterministic; avoid environment-specific assumptions so the tests remain stable in CI.

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Database naming conventions: snake_case fields for PostgreSQL compatibility
[assembly: SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Scope = "type", Target = "~T:FSH.Framework.Infrastructure.Auth.User", Justification = "Database field names follow snake_case convention")]

// Package version warnings: NuGet handles minor version differences automatically
[assembly: SuppressMessage("General", "NU1603:Package dependency downgrade", Justification = "Minor version differences are handled by NuGet automatically")] 
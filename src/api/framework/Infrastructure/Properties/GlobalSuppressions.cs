// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// ======================================================================
// DATABASE DESIGN PATTERNS - COMPATIBILITY REQUIREMENTS
// ======================================================================

// CA1707: Snake_case naming follows PostgreSQL database conventions
// JUSTIFICATION: Database field names must match PostgreSQL snake_case standard
// RISK: Code style inconsistency with C# naming conventions
// ALTERNATIVE: Use [Column] attributes for mapping but adds complexity
[assembly: SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Scope = "type", Target = "~T:FSH.Framework.Infrastructure.Auth.User", Justification = "Database field names follow snake_case convention")]

// ======================================================================
// PACKAGE MANAGEMENT - NUGET VERSION RESOLUTION
// ======================================================================

// NU1603: NuGet automatically resolves minor version differences
// JUSTIFICATION: Framework handles compatible version upgrades automatically
// RISK: Minimal - only affects minor version differences
// ALTERNATIVE: Pin exact versions but reduces flexibility for security updates
[assembly: SuppressMessage("General", "NU1603:Package dependency downgrade", Justification = "Minor version differences are handled by NuGet automatically")] 
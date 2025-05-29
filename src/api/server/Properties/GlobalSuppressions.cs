// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// ======================================================================
// ⚠️  HIGH RISK SUPPRESSIONS - REQUIRES CAREFUL REVIEW
// ======================================================================
// These suppressions may hide real issues and should be periodically reviewed

// CA1031: Generic exception handling suppressed globally
// JUSTIFICATION: Global exception handling middleware is intentional for API consistency
// ⚠️ RISK: May hide specific exceptions that should be handled differently
// ⚠️ ACTION REQUIRED: Replace with specific exception types where possible
// ALTERNATIVE: Implement specific exception handlers in controllers
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Global exception handling is intentional")]

// CA2254: Dynamic logging templates suppressed globally
// JUSTIFICATION: Using structured logging with Serilog for flexibility
// ⚠️ RISK: Performance impact and potential security vulnerabilities
// ⚠️ ACTION REQUIRED: Migrate to static LoggerMessage delegates for high-traffic paths
// ALTERNATIVE: Use LoggerMessage.Define for compile-time validation
[assembly: SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Dynamic logging templates are acceptable")]

// ======================================================================
// FRAMEWORK INTEGRATION - EXTERNAL DEPENDENCY REQUIREMENTS
// ======================================================================

// CA1848: High-performance logging patterns with Serilog
// JUSTIFICATION: Serilog provides structured logging without LoggerMessage delegates
// RISK: Minor performance impact in high-throughput scenarios
// ALTERNATIVE: Implement LoggerMessage.Define for critical paths
[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Using structured logging with Serilog")]

// EF1001: Internal Entity Framework APIs usage
// JUSTIFICATION: Required for advanced EF Core scenarios and migrations
// RISK: Breaking changes in EF Core updates
// ALTERNATIVE: Avoid if possible, monitor EF Core release notes
[assembly: SuppressMessage("Microsoft.EntityFrameworkCore", "EF1001:Internal EF Core API usage", Justification = "Required for advanced EF scenarios")]

// ======================================================================
// API DESIGN PATTERNS - PUBLIC CONTRACT REQUIREMENTS
// ======================================================================

// CA1515: API model classes must be public for model binding
// JUSTIFICATION: ASP.NET Core requires public classes for JSON serialization and model binding
// RISK: None - this is required by the framework
// ALTERNATIVE: None available while maintaining API functionality
[assembly: SuppressMessage("Design", "CA1515:Consider making public types internal", Scope = "namespaceanddescendants", Target = "~N:FSH.Starter.WebApi.Host", Justification = "API model classes need to be public for proper model binding and API documentation")]

// CA1062: Public method parameter validation suppressed for controllers
// JUSTIFICATION: ASP.NET Core model binding provides validation automatically
// RISK: None - framework validates parameters before reaching controller methods
// ALTERNATIVE: Manual null checks but redundant with model binding
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Scope = "namespaceanddescendants", Target = "~N:FSH.Starter.WebApi.Host.Controllers", Justification = "Controller parameters are validated by model binding")]

// ======================================================================
// DATABASE DESIGN PATTERNS - COMPATIBILITY REQUIREMENTS
// ======================================================================

// CA1707: Snake_case naming follows PostgreSQL database conventions
// JUSTIFICATION: Database field names must match PostgreSQL snake_case standard
// RISK: Code style inconsistency with C# naming conventions
// ALTERNATIVE: Use [Column] attributes for mapping but adds complexity
[assembly: SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Scope = "type", Target = "~T:FSH.Framework.Infrastructure.Auth.User", Justification = "Database field names follow snake_case convention")]

// ======================================================================
// DEVELOPMENT TOOLING - BUILD-TIME ANALYZERS
// ======================================================================

// SA1600: XML documentation suppressed for specific types
// JUSTIFICATION: Program class and migrations don't require XML documentation
// RISK: None - documentation not needed for these infrastructure types
// ALTERNATIVE: Add documentation but provides no value
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Scope = "type", Target = "~T:Program", Justification = "Program class documentation is not required")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Scope = "namespaceanddescendants", Target = "~N:FSH.Starter.WebApi.Host.Migrations", Justification = "Migration files don't require documentation")]

// SA1516: Blank line formatting rules relaxed
// JUSTIFICATION: StyleCop blank line requirements can be overly strict in some contexts
// RISK: Minor - affects only code formatting consistency
// ALTERNATIVE: Follow strict rules but may reduce readability in some cases
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1516:Elements should be separated by blank line", Justification = "Blank line requirements can be overly strict in some contexts")]

// ======================================================================
// PACKAGE MANAGEMENT & SECURITY SCANNING
// ======================================================================

// SCS9999: SecurityCodeScan legacy package warnings
// JUSTIFICATION: SecurityCodeScan is used for development-time security analysis
// RISK: None - only affects build-time analysis
// ALTERNATIVE: Use newer security analyzers when available
[assembly: SuppressMessage("Security", "SCS9999:Legacy package warning", Justification = "SecurityCodeScan package is used for security analysis during development and is acceptable")]

// NU1603: NuGet version resolution differences
// JUSTIFICATION: NuGet automatically resolves compatible minor version differences
// RISK: Minimal - only affects minor version differences
// ALTERNATIVE: Pin exact versions but reduces flexibility for security updates
[assembly: SuppressMessage("General", "NU1603:Package dependency downgrade", Justification = "Minor version differences are handled by NuGet automatically")]

// NetAnalyzersVersionWarning: Code analyzer version compatibility
// JUSTIFICATION: Using compatible analyzer versions that work with current SDK
// RISK: None - analyzer compatibility is maintained
// ALTERNATIVE: Update analyzers with each SDK update
[assembly: SuppressMessage("General", "NetAnalyzersVersionWarning", Justification = "Using compatible analyzer versions that work with the current SDK")] 

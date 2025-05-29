// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// General suppressions for common false positives
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Global exception handling is intentional")]
[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Using structured logging with Serilog")]
[assembly: SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Dynamic logging templates are acceptable")]

// Entity Framework specific suppressions
[assembly: SuppressMessage("Microsoft.EntityFrameworkCore", "EF1001:Internal EF Core API usage", Justification = "Required for advanced EF scenarios")]

// StyleCop suppressions for generated code
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Scope = "type", Target = "~T:Program", Justification = "Program class documentation is not required")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Scope = "namespaceanddescendants", Target = "~N:FSH.Starter.WebApi.Host.Migrations", Justification = "Migration files don't require documentation")]

// API Controller suppressions
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Scope = "namespaceanddescendants", Target = "~N:FSH.Starter.WebApi.Host.Controllers", Justification = "Controller parameters are validated by model binding")] 
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// ======================================================================
// SEARCH & FILTERING OPERATIONS - PERFORMANCE OPTIMIZATIONS
// ======================================================================

// CA1308: Search operations use ToLower() for case-insensitive matching
// JUSTIFICATION: ToLower() is more appropriate than ToUpper() for search operations
// RISK: Minimal - search functionality requires case-insensitive comparison
// ALTERNATIVE: ToUpperInvariant() could be used but offers no advantage in this context
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Scope = "member", Target = "~M:FSH.Framework.Core.Specifications.SpecificationBuilderExtensions.AddSearchPropertyByKeyword``1(Ardalis.Specification.ISpecificationBuilder{``0},System.Linq.Expressions.Expression,System.Linq.Expressions.ParameterExpression,System.String,System.String)", Justification = "ToLower is appropriate for search operations")]

// ======================================================================
// PUBLIC API DESIGN - EXTERNAL CONTRACT REQUIREMENTS
// ======================================================================

// CA1819: Array properties are part of the public API contract design
// JUSTIFICATION: These arrays are exposed as public API for filtering and search operations
// RISK: Arrays are mutable, but these are meant to be input parameters
// ALTERNATIVE: Use IReadOnlyCollection<T> but would break API compatibility
[assembly: SuppressMessage("Performance", "CA1819:Properties should not return arrays", Scope = "member", Target = "~P:FSH.Framework.Core.Paging.Search.Fields", Justification = "Array property is part of the public API contract")]
[assembly: SuppressMessage("Performance", "CA1819:Properties should not return arrays", Scope = "member", Target = "~P:FSH.Framework.Core.Paging.PaginationFilter.OrderBy", Justification = "Array property is part of the public API contract")] 
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Search operations: ToLower is more appropriate than ToUpper for case-insensitive search
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Scope = "member", Target = "~M:FSH.Framework.Core.Specifications.SpecificationBuilderExtensions.AddSearchPropertyByKeyword``1(Ardalis.Specification.ISpecificationBuilder{``0},System.Linq.Expressions.Expression,System.Linq.Expressions.ParameterExpression,System.String,System.String)", Justification = "ToLower is appropriate for search operations")]

// API public contract: These array properties are part of the public API design
[assembly: SuppressMessage("Performance", "CA1819:Properties should not return arrays", Scope = "member", Target = "~P:FSH.Framework.Core.Paging.Search.Fields", Justification = "Array property is part of the public API contract")]
[assembly: SuppressMessage("Performance", "CA1819:Properties should not return arrays", Scope = "member", Target = "~P:FSH.Framework.Core.Paging.PaginationFilter.OrderBy", Justification = "Array property is part of the public API contract")] 
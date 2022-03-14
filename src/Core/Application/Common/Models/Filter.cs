namespace FSH.WebApi.Application.Common.Models;

public static class FilterOperator
{
    public const string EQ = "eq";
    public const string NEQ = "neq";
    public const string LT = "lt";
    public const string LTE = "lte";
    public const string GT = "gt";
    public const string GTE = "gte";
    public const string BETWEEN = "between";
    public const string STARTSWITH = "startswith";
    public const string ENDSWITH = "endswith";
    public const string CONTAINS = "contains";
}

public static class FilterLogic
{
    public const string AND = "and";
    public const string OR = "or";
    public const string XOR = "xor";
}

public class Filter
{
    public string Field { get; set; } = default!;

    public string Operator { get; set; } = default!;

    public object Value { get; set; } = default!;

    public object? ValueAux { get; set; }

    public string Logic { get; set; } = default!;

    public IEnumerable<Filter> Filters { get; set; } = default!;
}
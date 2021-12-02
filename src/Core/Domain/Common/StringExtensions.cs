namespace DN.WebApi.Domain.Common;

public static class StringExtensions
{
    public static string NullToString(this object? Value)
        => Value?.ToString() ?? string.Empty;
}
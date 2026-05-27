using System.Text.RegularExpressions;

namespace FSH.CLI.Infrastructure;

/// <summary>
/// Validates project names for use as C# identifiers, folder names, and NuGet package names.
/// </summary>
internal static partial class ProjectNameValidator
{
    /// <summary>
    /// Returns null if the name is valid, or an error message describing the problem.
    /// </summary>
    internal static string? Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Project name cannot be empty.";

        if (name.Length > 128)
            return "Project name must be 128 characters or fewer.";

        if (!ValidNamePattern().IsMatch(name))
            return "Project name must start with a letter and contain only letters, digits, dots, hyphens, or underscores.";

        if (name.StartsWith('.') || name.EndsWith('.'))
            return "Project name cannot start or end with a dot.";

        if (name.Contains("..", StringComparison.Ordinal))
            return "Project name cannot contain consecutive dots.";

        if (FshConstants.ReservedNames.Contains(name))
            return $"'{name}' is a reserved name and cannot be used.";

        // Check for C# reserved keywords (common ones)
        if (IsCSharpKeyword(name))
            return $"'{name}' is a C# keyword and cannot be used as a project name.";

        return null;
    }

    private static bool IsCSharpKeyword(string name)
    {
        return name switch
        {
            "abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or "catch" or
            "char" or "checked" or "class" or "const" or "continue" or "decimal" or "default" or
            "delegate" or "do" or "double" or "else" or "enum" or "event" or "explicit" or "extern" or
            "false" or "finally" or "fixed" or "float" or "for" or "foreach" or "goto" or "if" or
            "implicit" or "in" or "int" or "interface" or "internal" or "is" or "lock" or "long" or
            "namespace" or "new" or "null" or "object" or "operator" or "out" or "override" or "params" or
            "private" or "protected" or "public" or "readonly" or "ref" or "return" or "sbyte" or
            "sealed" or "short" or "sizeof" or "static" or "string" or "struct" or "switch" or "this" or
            "throw" or "true" or "try" or "typeof" or "uint" or "ulong" or "unchecked" or "unsafe" or
            "ushort" or "using" or "virtual" or "void" or "volatile" or "while" => true,
            _ => false
        };
    }

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9._\-]*$")]
    private static partial Regex ValidNamePattern();
}

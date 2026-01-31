using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Parses template syntax and normalizes project names
/// </summary>
[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Lowercase is required for Docker, Terraform, and GitHub Actions naming conventions")]
internal sealed class TemplateParser : ITemplateParser
{
    private static readonly Regex VariablePattern = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);
    private static readonly Regex ValidTemplatePattern = new(@"^\s*(\{\{[^}]+\}\}|[^{])*\s*$", RegexOptions.Compiled);

    public IEnumerable<string> ExtractVariables(string template)
    {
        ArgumentException.ThrowIfNullOrEmpty(template);

        var matches = VariablePattern.Matches(template);
        return matches.Select(m => m.Groups[1].Value.Trim()).Distinct();
    }

    public bool IsValidTemplate(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return false;
        }

        return ValidTemplatePattern.IsMatch(template);
    }

    public string NormalizeProjectName(string projectName, NameContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectName);

        return context switch
        {
            NameContext.LowerCase => projectName.ToLowerInvariant(),
            NameContext.SafeIdentifier => MakeSafeIdentifier(projectName),
            NameContext.DockerImage => MakeDockerImageName(projectName),
            NameContext.DatabaseName => MakeDatabaseName(projectName),
            NameContext.Default => projectName,
            _ => throw new ArgumentOutOfRangeException(nameof(context), context, "Unknown name context")
        };
    }

    private static string MakeSafeIdentifier(string name)
    {
        // Replace dots with underscores for safe C# identifiers
        return name.Replace(".", "_", StringComparison.Ordinal);
    }

    private static string MakeDockerImageName(string name)
    {
        // Docker image names must be lowercase
        return name.ToUpperInvariant().ToLowerInvariant();
    }

    private static string MakeDatabaseName(string name)
    {
        // Database names should be lowercase and use underscores
        return name.ToLowerInvariant().Replace(".", "_", StringComparison.Ordinal);
    }
}
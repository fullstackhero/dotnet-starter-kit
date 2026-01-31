using FSH.CLI.Models;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Parses template syntax and extracts variables
/// </summary>
internal interface ITemplateParser
{
    /// <summary>
    /// Extracts variables from a template string
    /// </summary>
    IEnumerable<string> ExtractVariables(string template);

    /// <summary>
    /// Validates template syntax
    /// </summary>
    bool IsValidTemplate(string template);

    /// <summary>
    /// Normalizes project names for different contexts (lowercase, safe characters, etc.)
    /// </summary>
    string NormalizeProjectName(string projectName, NameContext context);
}

/// <summary>
/// Context for project name normalization
/// </summary>
internal enum NameContext
{
    Default,
    LowerCase,
    SafeIdentifier,
    DockerImage,
    DatabaseName
}
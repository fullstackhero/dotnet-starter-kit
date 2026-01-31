using FSH.CLI.Models;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Loads templates from various sources (embedded resources, disk, etc.)
/// </summary>
internal interface ITemplateLoader
{
    /// <summary>
    /// Gets the framework version for package references
    /// </summary>
    string GetFrameworkVersion();

    /// <summary>
    /// Gets a static template by name
    /// </summary>
    string GetStaticTemplate(string templateName);

    /// <summary>
    /// Checks if a template exists
    /// </summary>
    bool TemplateExists(string templateName);
}
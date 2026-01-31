using FSH.CLI.Models;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Validates template structure and configuration
/// </summary>
internal interface ITemplateValidator
{
    /// <summary>
    /// Validates project options for template generation
    /// </summary>
    ValidationResult ValidateProjectOptions(ProjectOptions options);

    /// <summary>
    /// Validates that required templates are available
    /// </summary>
    ValidationResult ValidateTemplateAvailability(ProjectOptions options);

    /// <summary>
    /// Validates generated template content
    /// </summary>
    ValidationResult ValidateGeneratedContent(string content, string templateType);

    /// <summary>
    /// Validates project structure compatibility
    /// </summary>
    ValidationResult ValidateProjectStructure(ProjectOptions options);
}

/// <summary>
/// Result of a validation operation
/// </summary>
internal record ValidationResult(bool IsValid, IEnumerable<string> Errors, IEnumerable<string> Warnings)
{
    public static ValidationResult Success() => new(true, Enumerable.Empty<string>(), Enumerable.Empty<string>());
    
    public static ValidationResult Failure(params string[] errors) => new(false, errors, Enumerable.Empty<string>());
    
    public static ValidationResult Warning(params string[] warnings) => new(true, Enumerable.Empty<string>(), warnings);
}
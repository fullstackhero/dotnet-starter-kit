using FSH.CLI.Models;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Validates template structure and project configuration
/// </summary>
internal sealed class TemplateValidator : ITemplateValidator
{
    private readonly ITemplateLoader _templateLoader;
    private readonly ITemplateParser _templateParser;

    public TemplateValidator(ITemplateLoader templateLoader, ITemplateParser templateParser)
    {
        _templateLoader = templateLoader ?? throw new ArgumentNullException(nameof(templateLoader));
        _templateParser = templateParser ?? throw new ArgumentNullException(nameof(templateParser));
    }

    public ValidationResult ValidateProjectOptions(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate project name
        if (string.IsNullOrWhiteSpace(options.Name))
        {
            errors.Add("Project name cannot be empty");
        }
        else if (options.Name.Length < 2)
        {
            errors.Add("Project name must be at least 2 characters long");
        }
        else if (options.Name.Length > 100)
        {
            errors.Add("Project name cannot exceed 100 characters");
        }

        // Validate project name characters
        if (!string.IsNullOrEmpty(options.Name) && 
            options.Name.Any(c => !char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_'))
        {
            errors.Add("Project name can only contain letters, digits, dots, hyphens, and underscores");
        }

        // Validate output path
        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            errors.Add("Output path cannot be empty");
        }
        else
        {
            try
            {
                _ = Path.GetFullPath(options.OutputPath);
            }
            catch (Exception)
            {
                errors.Add("Output path is not valid");
            }
        }

        // Validate architecture and project type combinations
        if (options.Architecture == ArchitectureStyle.Serverless && options.Type == ProjectType.ApiBlazor)
        {
            warnings.Add("Blazor WebAssembly with serverless architecture may require additional configuration for static file hosting");
        }

        // Validate database and architecture combinations
        if (options.Architecture == ArchitectureStyle.Serverless && options.Database == DatabaseProvider.SQLite)
        {
            warnings.Add("SQLite with serverless architecture may have limitations in multi-instance scenarios");
        }

        // Validate framework version if specified
        if (!string.IsNullOrEmpty(options.FrameworkVersion))
        {
            if (!IsValidFrameworkVersion(options.FrameworkVersion))
            {
                errors.Add($"Framework version '{options.FrameworkVersion}' is not in a valid format");
            }
        }

        return new ValidationResult(errors.Count == 0, errors, warnings);
    }

    public ValidationResult ValidateTemplateAvailability(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        // Check required static templates
        var requiredTemplates = new[]
        {
            "GlobalJson",
            "Gitignore",
            "EditorConfig"
        };

        foreach (var template in requiredTemplates)
        {
            if (!_templateLoader.TemplateExists(template))
            {
                errors.Add($"Required template '{template}' is not available");
            }
        }

        // Check Blazor-specific templates if needed
        if (options.Type == ProjectType.ApiBlazor)
        {
            var blazorTemplates = new[] { "BlazorCsproj", "BlazorApp" };
            foreach (var template in blazorTemplates)
            {
                if (!_templateLoader.TemplateExists(template))
                {
                    errors.Add($"Required Blazor template '{template}' is not available");
                }
            }
        }

        return new ValidationResult(errors.Count == 0, errors, Enumerable.Empty<string>());
    }

    public ValidationResult ValidateGeneratedContent(string content, string templateType)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentException.ThrowIfNullOrEmpty(templateType);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Basic content validation
        if (content.Length == 0)
        {
            errors.Add("Generated content is empty");
            return new ValidationResult(false, errors, warnings);
        }

        // Template-specific validations
        switch (templateType.ToUpperInvariant())
        {
            case "JSON":
                if (!IsValidJson(content))
                {
                    errors.Add("Generated JSON content is not valid");
                }
                break;

            case "XML":
            case "CSPROJ":
                if (!IsValidXml(content))
                {
                    errors.Add("Generated XML content is not valid");
                }
                break;

            case "DOCKERFILE":
                if (!content.Contains("FROM ", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add("Dockerfile doesn't contain a FROM instruction");
                }
                break;
        }

        // Check for unresolved template variables
        var unresolvedVariables = _templateParser.ExtractVariables(content);
        if (unresolvedVariables.Any())
        {
            warnings.Add($"Content contains unresolved variables: {string.Join(", ", unresolvedVariables)}");
        }

        return new ValidationResult(errors.Count == 0, errors, warnings);
    }

    public ValidationResult ValidateProjectStructure(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var warnings = new List<string>();

        // Validate Docker compatibility
        if (options.IncludeDocker && options.Database == DatabaseProvider.SQLite)
        {
            warnings.Add("SQLite with Docker may require volume mounting for data persistence");
        }

        // Validate Aspire compatibility
        if (options.IncludeAspire && options.Architecture == ArchitectureStyle.Serverless)
        {
            warnings.Add("Aspire orchestration may not be suitable for serverless deployments");
        }

        // Validate module structure
        if (options.IncludeSampleModule && options.Architecture == ArchitectureStyle.Microservices)
        {
            warnings.Add("Sample module in microservices architecture should be moved to separate service");
        }

        return new ValidationResult(true, Enumerable.Empty<string>(), warnings);
    }

    private static bool IsValidFrameworkVersion(string version)
    {
        return Version.TryParse(version.Split('-')[0], out _);
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidXml(string xml)
    {
        try
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
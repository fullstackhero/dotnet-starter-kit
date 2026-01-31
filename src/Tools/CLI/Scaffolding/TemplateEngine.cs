using System.Diagnostics.CodeAnalysis;
using FSH.CLI.Models;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Refactored TemplateEngine that delegates to focused services
/// This maintains the original public API while using the new architecture
/// </summary>
[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Lowercase is required for Docker, Terraform, and GitHub Actions naming conventions")]
internal static class TemplateEngine
{
    private static readonly ITemplateRenderer _renderer = TemplateServices.GetRenderer();
    private static readonly ITemplateValidator _validator = TemplateServices.GetValidator();

    #region Solution and Project Templates

    public static string GenerateSolution(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderSolution(options);
    }

    public static string GenerateApiCsproj(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderApiCsproj(options);
    }

    public static string GenerateApiProgram(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderApiProgram(options);
    }

    public static string GenerateMigrationsCsproj(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderMigrationsCsproj(options);
    }

    public static string GenerateBlazorCsproj()
    {
        return _renderer.RenderBlazorCsproj();
    }

    public static string GenerateBlazorProgram(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderBlazorProgram(options);
    }

    public static string GenerateAppHostCsproj(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderAppHostCsproj(options);
    }

    public static string GenerateAppHostProgram(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderAppHostProgram(options);
    }

    #endregion

    #region Configuration Templates

    public static string GenerateAppSettings(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderAppSettings(options);
    }

    public static string GenerateAppSettingsDevelopment()
    {
        return _renderer.RenderAppSettingsDevelopment();
    }

    public static string GenerateApiLaunchSettings(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderApiLaunchSettings(options);
    }

    public static string GenerateAppHostLaunchSettings(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderAppHostLaunchSettings(options);
    }

    #endregion

    #region Blazor Templates

    public static string GenerateBlazorApp()
    {
        return _renderer.RenderBlazorApp();
    }

    public static string GenerateBlazorImports(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderBlazorImports(options);
    }

    public static string GenerateBlazorIndexPage(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderBlazorIndexPage(options);
    }

    public static string GenerateBlazorMainLayout(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderBlazorMainLayout(options);
    }

    #endregion

    #region Infrastructure Templates

    public static string GenerateDockerfile(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderDockerfile(options);
    }

    public static string GenerateDockerCompose(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderDockerCompose(options);
    }

    public static string GenerateDockerComposeOverride()
    {
        return _renderer.RenderDockerComposeOverride();
    }

    public static string GenerateTerraformMain(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderTerraformMain(options);
    }

    public static string GenerateTerraformVariables(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderTerraformVariables(options);
    }

    public static string GenerateTerraformOutputs(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderTerraformOutputs(options);
    }

    public static string GenerateGitHubActionsCI(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderGitHubActionsCI(options);
    }

    #endregion

    #region Module Templates

    public static string GenerateCatalogModule(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderCatalogModule(options);
    }

    public static string GenerateCatalogModuleCsproj(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderCatalogModuleCsproj(options);
    }

    public static string GenerateCatalogContractsCsproj()
    {
        return _renderer.RenderCatalogContractsCsproj();
    }

    public static string GenerateGetProductsEndpoint(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderGetProductsEndpoint(options);
    }

    #endregion

    #region Static Content Templates

    public static string GenerateReadme(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderReadme(options);
    }

    public static string GenerateGitignore()
    {
        return _renderer.RenderGitignore();
    }

    public static string GenerateDirectoryBuildProps(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderDirectoryBuildProps(options);
    }

    public static string GenerateDirectoryPackagesProps(ProjectOptions options)
    {
        ValidateOptions(options);
        return _renderer.RenderDirectoryPackagesProps(options);
    }

    public static string GenerateEditorConfig()
    {
        return _renderer.RenderEditorConfig();
    }

    public static string GenerateGlobalJson()
    {
        return _renderer.RenderGlobalJson();
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates project options before template generation
    /// </summary>
    private static void ValidateOptions(ProjectOptions options)
    {
        var validationResult = _validator.ValidateProjectOptions(options);
        
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid project options: {string.Join(", ", validationResult.Errors)}", nameof(options));
        }

        // Log warnings if any
        foreach (var warning in validationResult.Warnings)
        {
            Console.WriteLine($"Warning: {warning}");
        }
    }

    #endregion
}
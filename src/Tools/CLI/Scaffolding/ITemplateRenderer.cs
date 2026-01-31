using FSH.CLI.Models;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Renders templates with variable substitution
/// </summary>
internal interface ITemplateRenderer
{
    // Solution and Project Templates
    string RenderSolution(ProjectOptions options);
    string RenderApiCsproj(ProjectOptions options);
    string RenderApiProgram(ProjectOptions options);
    string RenderMigrationsCsproj(ProjectOptions options);
    string RenderBlazorCsproj();
    string RenderBlazorProgram(ProjectOptions options);
    string RenderAppHostCsproj(ProjectOptions options);
    string RenderAppHostProgram(ProjectOptions options);

    // Configuration Templates
    string RenderAppSettings(ProjectOptions options);
    string RenderAppSettingsDevelopment();
    string RenderApiLaunchSettings(ProjectOptions options);
    string RenderAppHostLaunchSettings(ProjectOptions options);

    // Blazor Templates
    string RenderBlazorApp();
    string RenderBlazorImports(ProjectOptions options);
    string RenderBlazorIndexPage(ProjectOptions options);
    string RenderBlazorMainLayout(ProjectOptions options);

    // Infrastructure Templates
    string RenderDockerfile(ProjectOptions options);
    string RenderDockerCompose(ProjectOptions options);
    string RenderDockerComposeOverride();
    string RenderTerraformMain(ProjectOptions options);
    string RenderTerraformVariables(ProjectOptions options);
    string RenderTerraformOutputs(ProjectOptions options);
    string RenderGitHubActionsCI(ProjectOptions options);

    // Module Templates
    string RenderCatalogModule(ProjectOptions options);
    string RenderCatalogModuleCsproj(ProjectOptions options);
    string RenderCatalogContractsCsproj();
    string RenderGetProductsEndpoint(ProjectOptions options);

    // Static Content Templates
    string RenderReadme(ProjectOptions options);
    string RenderGitignore();
    string RenderDirectoryBuildProps(ProjectOptions options);
    string RenderDirectoryPackagesProps(ProjectOptions options);
    string RenderEditorConfig();
    string RenderGlobalJson();
}
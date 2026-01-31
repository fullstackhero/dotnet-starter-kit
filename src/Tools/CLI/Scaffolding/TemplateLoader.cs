using System.Reflection;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// Loads templates from embedded resources and static sources
/// </summary>
internal sealed class TemplateLoader : ITemplateLoader
{
    private static readonly Lazy<string> _frameworkVersion = new(GetFrameworkVersionInternal);
    
    private static readonly Dictionary<string, string> _staticTemplates = new()
    {
        ["AppSettingsDevelopment"] = """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Debug",
              "Microsoft.AspNetCore": "Information",
              "Microsoft.EntityFrameworkCore": "Warning"
            }
          }
        }
        """,
        
        ["BlazorCsproj"] = """
        <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" />
            <PackageReference Include="Microsoft.AspNetCore.Components.Authorization" />
            <PackageReference Include="MudBlazor" />
            <PackageReference Include="FullStackHero.Framework.Blazor.UI" />
          </ItemGroup>

        </Project>
        """,
        
        ["BlazorApp"] = """
        <MudThemeProvider />
        <MudPopoverProvider />
        <MudDialogProvider />
        <MudSnackbarProvider />

        <Router AppAssembly="@typeof(App).Assembly">
            <Found Context="routeData">
                <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
                <FocusOnNavigate RouteData="@routeData" Selector="h1" />
            </Found>
            <NotFound>
                <PageTitle>Not found</PageTitle>
                <LayoutView Layout="@typeof(MainLayout)">
                    <MudText Typo="Typo.h6">Sorry, there's nothing at this address.</MudText>
                </LayoutView>
            </NotFound>
        </Router>
        """,
        
        ["DockerComposeOverride"] = """
        version: '3.8'

        # Development overrides
        services:
          redis:
            command: redis-server --appendonly yes
        """,
        
        ["CatalogContractsCsproj"] = """
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>

        </Project>
        """,
        
        ["GlobalJson"] = """
        {
          "sdk": {
            "version": "10.0.100",
            "rollForward": "latestFeature"
          }
        }
        """,
        
        ["Gitignore"] = """
        ## .NET
        bin/
        obj/
        *.user
        *.userosscache
        *.suo
        *.cache
        *.nupkg

        ## IDE
        .vs/
        .vscode/
        .idea/
        *.swp
        *.swo

        ## Build
        publish/
        artifacts/
        TestResults/

        ## Secrets
        appsettings.*.json
        !appsettings.json
        !appsettings.Development.json
        *.pfx
        *.p12

        ## Terraform
        .terraform/
        *.tfstate
        *.tfstate.*
        .terraform.lock.hcl

        ## OS
        .DS_Store
        Thumbs.db

        ## Logs
        *.log
        logs/
        """,
        
        ["EditorConfig"] = """
        # EditorConfig is awesome: https://EditorConfig.org

        root = true

        [*]
        indent_style = space
        indent_size = 4
        end_of_line = lf
        charset = utf-8
        trim_trailing_whitespace = true
        insert_final_newline = true

        [*.{cs,csx}]
        indent_size = 4

        [*.{json,yml,yaml}]
        indent_size = 2

        [*.md]
        trim_trailing_whitespace = false

        [*.razor]
        indent_size = 4

        # C# files
        [*.cs]

        # Sort using and Import directives with System.* appearing first
        dotnet_sort_system_directives_first = true
        dotnet_separate_import_directive_groups = false

        # Avoid "this." for fields, properties, methods, events
        dotnet_style_qualification_for_field = false:suggestion
        dotnet_style_qualification_for_property = false:suggestion
        dotnet_style_qualification_for_method = false:suggestion
        dotnet_style_qualification_for_event = false:suggestion

        # Use language keywords instead of framework type names
        dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
        dotnet_style_predefined_type_for_member_access = true:suggestion

        # Prefer var
        csharp_style_var_for_built_in_types = true:suggestion
        csharp_style_var_when_type_is_apparent = true:suggestion
        csharp_style_var_elsewhere = true:suggestion

        # Prefer expression-bodied members
        csharp_style_expression_bodied_methods = when_on_single_line:suggestion
        csharp_style_expression_bodied_constructors = when_on_single_line:suggestion
        csharp_style_expression_bodied_properties = when_on_single_line:suggestion

        # Prefer pattern matching
        csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
        csharp_style_pattern_matching_over_as_with_null_check = true:suggestion

        # Namespace preferences
        csharp_style_namespace_declarations = file_scoped:suggestion

        # Newline preferences
        csharp_new_line_before_open_brace = all
        csharp_new_line_before_else = true
        csharp_new_line_before_catch = true
        csharp_new_line_before_finally = true
        """
    };

    public string GetFrameworkVersion() => _frameworkVersion.Value;

    public string GetStaticTemplate(string templateName)
    {
        if (!_staticTemplates.TryGetValue(templateName, out var template))
        {
            throw new InvalidOperationException($"Static template '{templateName}' not found.");
        }
        
        return template;
    }

    public bool TemplateExists(string templateName)
    {
        return _staticTemplates.ContainsKey(templateName);
    }

    private static string GetFrameworkVersionInternal()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "10.0.0";

        // Remove any +buildmetadata suffix (e.g., "10.0.0-rc.1+abc123" -> "10.0.0-rc.1")
        var plusIndex = version.IndexOf('+', StringComparison.Ordinal);
        return plusIndex > 0 ? version[..plusIndex] : version;
    }
}
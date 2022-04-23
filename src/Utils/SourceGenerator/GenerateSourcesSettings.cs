namespace FSH.WebApi.Utils.SourceGenerator;

public class GenerateSourcesSettings
{
    public bool RunSourceSettings { get; set; } = true;
    public string PathToTxtFiles { get; set; } = @"Utils\SourceGenerator\BasicSources\";
    public string PathToData { get; set; } = @"Core\Domain\Catalog\";
    public string StringNameSpace { get; set; } = "FSH.WebApi.Application.Catalog.";
    public string PathToControllers { get; set; } = @"Host\Controllers\Catalog\";

    public string ControllersNamespace { get; set; } = "FSH.WebApi.Host.Controllers.Catalog";
    public string PathToApplicationCatalog { get; set; } = @"Core\Application\Catalog\";

    public string DetermineDomainEntityPath { get; set; } = "Domain.Catalog";
    public string EventsUsing { get; set; } = "FSH.WebApi.Domain.Common.Events";
    public string PathToPermissions { get; set; } = @"Core\Shared\Authorization\FSHPermissions.cs";

}

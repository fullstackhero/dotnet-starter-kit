using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Web.Modules;
using FSH.Modules.SchoolManagement.Features.v1.Ecoles.CreateEcole;
using FSH.Modules.SchoolManagement.Features.v1.Ecoles.UpdateEcole;
using FSH.Modules.SchoolManagement.Features.v1.Ecoles.DeleteEcole;
using FSH.Modules.SchoolManagement.Features.v1.Ecoles.GetEcoles;
using FSH.Modules.SchoolManagement.Features.v1.Ecoles.GetEcoleById;
using FSH.Modules.SchoolManagement.Features.v1.Classes.CreateClasse;
using FSH.Modules.SchoolManagement.Features.v1.Classes.UpdateClasse;
using FSH.Modules.SchoolManagement.Features.v1.Classes.DeleteClasse;
using FSH.Modules.SchoolManagement.Features.v1.Classes.GetClasses;
using FSH.Modules.SchoolManagement.Features.v1.Classes.GetClasseById;
using FSH.Modules.SchoolManagement.Features.v1.Matieres.CreateMatiere;
using FSH.Modules.SchoolManagement.Features.v1.Matieres.UpdateMatiere;
using FSH.Modules.SchoolManagement.Features.v1.Matieres.GetMatieres;
using FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.CreateAnneeScolaire;
using FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.UpdateAnneeScolaire;
using FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.GetAnneeScolaires;
using FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.GetAnneeScolaireActive;
using FSH.Modules.SchoolManagement.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.SchoolManagement;

public class SchoolManagementModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddHeroDbContext<SchoolDbContext>();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<SchoolDbContext>(
                name: "db:school",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/school")
            .WithTags("School")
            .WithApiVersionSet(apiVersionSet);

        // Ecoles
        group.MapCreateEcoleEndpoint();
        group.MapUpdateEcoleEndpoint();
        group.MapDeleteEcoleEndpoint();
        group.MapGetEcolesEndpoint();
        group.MapGetEcoleByIdEndpoint();

        // Classes
        group.MapCreateClasseEndpoint();
        group.MapUpdateClasseEndpoint();
        group.MapDeleteClasseEndpoint();
        group.MapGetClassesEndpoint();
        group.MapGetClasseByIdEndpoint();

        // Matieres
        group.MapCreateMatiereEndpoint();
        group.MapUpdateMatiereEndpoint();
        group.MapGetMatieresEndpoint();

        // Annees Scolaires
        group.MapCreateAnneeScolaireEndpoint();
        group.MapUpdateAnneeScolaireEndpoint();
        group.MapGetAnneeScolairesEndpoint();
        group.MapGetAnneeScolaireActiveEndpoint();
    }
}

using FSH.Framework.Shared.Persistence;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Classes.GetClasses;

public sealed class GetClassesQuery : IPagedQuery, IQuery<PagedResponse<ClasseDto>>
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
    public Guid? EcoleId { get; set; }
    public string? Niveau { get; set; }
    public Guid? AnneeScolaireId { get; set; }
}

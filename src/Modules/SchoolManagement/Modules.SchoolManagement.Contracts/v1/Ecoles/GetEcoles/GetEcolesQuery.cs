using FSH.Framework.Shared.Persistence;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.GetEcoles;

public sealed class GetEcolesQuery : IPagedQuery, IQuery<PagedResponse<EcoleDto>>
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
    public string? Search { get; set; }
    public string? Region { get; set; }
    public string? Type { get; set; }
}

using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Roles.GetRoles;

public sealed class GetRolesQuery : IPagedQuery, IQuery<PagedResponse<RoleDto>>
{
    public int? PageNumber { get; set; }

    public int? PageSize { get; set; }

    public string? Sort { get; set; }

    /// <summary>Case-insensitive substring match against role name + description.</summary>
    public string? Search { get; set; }
}

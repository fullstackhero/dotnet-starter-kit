using DN.WebApi.Shared.DTOs.Filters;

namespace DN.WebApi.Shared.DTOs.Identity;

public class UserListFilter : PaginationFilter
{
    public bool? IsActive { get; set; }
}
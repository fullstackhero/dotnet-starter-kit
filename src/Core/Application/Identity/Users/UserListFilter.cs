using DN.WebApi.Application.Common.Filters;

namespace DN.WebApi.Application.Identity.Users;

public class UserListFilter : PaginationFilter
{
    public bool? IsActive { get; set; }
}
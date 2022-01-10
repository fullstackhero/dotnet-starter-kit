using DN.WebApi.Application.Common.Models;

namespace DN.WebApi.Application.Identity.Users;

public class UserListFilter : PaginationFilter
{
    public bool? IsActive { get; set; }
}
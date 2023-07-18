namespace FL_CRMS_ERP_WEBAPI.Application.Identity.Users;

public class UserListFilter : PaginationFilter
{
    public bool? IsActive { get; set; }
}
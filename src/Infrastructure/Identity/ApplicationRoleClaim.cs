using Microsoft.AspNetCore.Identity;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Identity;

public class ApplicationRoleClaim : IdentityRoleClaim<string>
{
    public string? CreatedBy { get; init; }
    public DateTime CreatedOn { get; init; }
}
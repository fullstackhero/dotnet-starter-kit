using Microsoft.AspNetCore.Identity;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public string? ReportTo { get; set; }

    public ApplicationRole(string name, string? description = null, string? reportTo = null)
        : base(name)
    {
        Description = description;
        NormalizedName = name.ToUpperInvariant();
        ReportTo = reportTo;
    }
}
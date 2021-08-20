using Microsoft.AspNetCore.Identity;

namespace DN.WebApi.Infrastructure.Identity.Models
{
    public class ExtendedRoleClaim : IdentityRoleClaim<string>
    {
        public string Description { get; set; }
    }
}
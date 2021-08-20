using Microsoft.AspNetCore.Identity;

namespace DN.WebApi.Infrastructure.Identity.Models
{
    public class ExtendedRole : IdentityRole
    {
        public string Description { get; set; }
    }
}
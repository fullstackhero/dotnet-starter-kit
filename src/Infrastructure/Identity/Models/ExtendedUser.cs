using Microsoft.AspNetCore.Identity;

namespace DN.WebApi.Infrastructure.Identity.Models
{
    public class ExtendedUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Persistence
{
    public abstract class BaseDbContext : IdentityDbContext<ExtendedUser, ExtendedRole, string, IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>, ExtendedRoleClaim, IdentityUserToken<string>>
    {
        protected BaseDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
            modelBuilder.ApplyIdentityConfiguration();
        }
    }
}
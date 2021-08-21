using DN.WebApi.Application.Settings;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Persistence
{
    public abstract class BaseDbContext : IdentityDbContext<ExtendedUser, ExtendedRole, string, IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>, ExtendedRoleClaim, IdentityUserToken<string>>
    {
        private readonly DbSettings _dbSettings;
        protected BaseDbContext(DbContextOptions options, IOptions<DbSettings> dbSettings) : base(options)
        {
            _dbSettings = dbSettings.Value;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
            modelBuilder.ApplyDefaultConfiguration(_dbSettings);
            modelBuilder.ApplyIdentityConfiguration();
        }
    }
}
using DN.WebApi.Application.Abstractions.Services.General;
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
        private readonly ITenantService _tenantService;
        protected BaseDbContext(DbContextOptions options, ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
            //modelBuilder.ApplyDefaultConfiguration(_tenantSettings);
            modelBuilder.ApplyIdentityConfiguration();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var tenantConnectionString = _tenantService.GetConnectionString();
            if (!string.IsNullOrEmpty(tenantConnectionString))
            {
                optionsBuilder.UseNpgsql(_tenantService.GetConnectionString());
            }
        }
    }
}
using DN.WebApi.Application.Configurations;
using DN.WebApi.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Persistence.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void ApplyDefaultConfiguration(this ModelBuilder builder, PersistenceConfiguration config)
        {
            if (config.UseMsSql)
            {
                foreach (var property in builder.Model.GetEntityTypes()
                    .SelectMany(t => t.GetProperties())
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
                {
                    property.SetColumnType("decimal(23,2)");
                }
            }
        }
        public static void ApplyIdentityConfiguration(this ModelBuilder builder)
        {
            builder.Entity<ExtendedUser>(entity =>
            {
                entity.ToTable(name: "Users","Identity");
            });
            builder.Entity<ExtendedRole>(entity =>
            {
                entity.ToTable(name: "Roles","Identity");
            });
            builder.Entity<ExtendedRoleClaim>(entity =>
            {
                entity.ToTable(name: "RoleClaims","Identity");
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles","Identity");
            });

            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims","Identity");
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins","Identity");
            });
            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens","Identity");
            });
        
        }
    }
}
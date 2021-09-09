using System.Linq.Expressions;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace DN.WebApi.Infrastructure.Persistence.Extensions
{
    public static class ModelBuilderExtensions
    {

        public static void ApplyIdentityConfiguration(this ModelBuilder builder, ITenantService _tenantService)
        {
            var dbProvider = _tenantService.GetDatabaseProvider();
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable(name: "Users", dbProvider.ToLower() == "mysql" ? null : "Identity");
            });
            builder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable(name: "Roles", dbProvider.ToLower() == "mysql" ? null : "Identity");
                entity.Metadata.RemoveIndex(new[] { entity.Property(r => r.NormalizedName).Metadata });
                entity.HasIndex(r => new { r.NormalizedName, r.TenantId }).HasDatabaseName("RoleNameIndex").IsUnique();
            });
            builder.Entity<ApplicationRoleClaim>(entity =>
            {
                entity.ToTable(name: "RoleClaims", dbProvider.ToLower() == "mysql" ? null : "Identity");
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles", dbProvider.ToLower() == "mysql" ? null : "Identity");
            });

            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims", dbProvider.ToLower() == "mysql" ? null : "Identity");
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins", dbProvider.ToLower() == "mysql" ? null : "Identity");
            });
            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens", dbProvider.ToLower() == "mysql" ? null : "Identity");
            });
        }

        public static void ApplyGlobalFilters<TInterface>(this ModelBuilder modelBuilder, Expression<Func<TInterface, bool>> expression)
        {
            var entities = modelBuilder.Model
                .GetEntityTypes()
                .Where(e => e.ClrType.GetInterface(typeof(TInterface).Name) != null)
                .Select(e => e.ClrType);
            foreach (var entity in entities)
            {
                var newParam = Expression.Parameter(entity);
                var newbody = ReplacingExpressionVisitor.Replace(expression.Parameters.Single(), newParam, expression.Body);
                modelBuilder.Entity(entity).HasQueryFilter(Expression.Lambda(newbody, newParam));
            }
        }
    }
}
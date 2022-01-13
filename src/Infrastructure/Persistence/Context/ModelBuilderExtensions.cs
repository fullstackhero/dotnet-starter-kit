using System.Linq.Expressions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using FSH.WebApi.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace FSH.WebApi.Infrastructure.Persistence.Context;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyIdentityConfiguration(this ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>()
            .ToTable("Users", "IDENTITY")
            .Property(u => u.ObjectId)
                .HasMaxLength(256);

        builder.Entity<ApplicationRole>()
            .ToTable("Roles", "IDENTITY");

        builder.Entity<ApplicationRoleClaim>()
            .ToTable("RoleClaims", "IDENTITY");

        builder.Entity<IdentityUserRole<string>>()
            .ToTable("UserRoles", "IDENTITY");

        builder.Entity<IdentityUserClaim<string>>()
            .ToTable("UserClaims", "IDENTITY");

        builder.Entity<IdentityUserLogin<string>>()
            .ToTable("UserLogins", "IDENTITY");

        builder.Entity<IdentityUserToken<string>>()
            .ToTable("UserTokens", "IDENTITY");

        // Make identity tables multi-tenant
        builder.Entity<ApplicationUser>().IsMultiTenant();
        builder.Entity<ApplicationRole>().IsMultiTenant().AdjustUniqueIndexes();
        builder.Entity<ApplicationRoleClaim>().IsMultiTenant();
        builder.Entity<IdentityUserRole<string>>().IsMultiTenant();
        builder.Entity<IdentityUserClaim<string>>().IsMultiTenant();
        builder.Entity<IdentityUserLogin<string>>().IsMultiTenant();
        builder.Entity<IdentityUserToken<string>>().IsMultiTenant();

        return builder;
    }

    public static ModelBuilder AppendGlobalQueryFilter<TInterface>(this ModelBuilder modelBuilder, Expression<Func<TInterface, bool>> expression)
    {
        // gets a list of entities that implement the interface TInterface
        var entities = modelBuilder.Model
            .GetEntityTypes()
            .Where(e => e.ClrType.GetInterface(typeof(TInterface).Name) is not null)
            .Select(e => e.ClrType);

        foreach (var entity in entities)
        {
            var parameterType = Expression.Parameter(modelBuilder.Entity(entity).Metadata.ClrType);
            var expressionFilter = ReplacingExpressionVisitor.Replace(expression.Parameters.Single(), parameterType, expression.Body);

            // get existing query filters of the entity(s)
            var currentQueryFilter = modelBuilder.Entity(entity).Metadata?.GetQueryFilter();
            if (currentQueryFilter is not null)
            {
                var currentExpressionFilter = ReplacingExpressionVisitor.Replace(currentQueryFilter.Parameters.Single(), parameterType, currentQueryFilter.Body);

                // Append new query filter with the existing filter
                expressionFilter = Expression.AndAlso(currentExpressionFilter, expressionFilter);
            }

            var lambdaExpression = Expression.Lambda(expressionFilter, parameterType);

            // applies the filter to the entity(s)
            modelBuilder.Entity(entity).HasQueryFilter(lambdaExpression);
        }

        return modelBuilder;
    }
}
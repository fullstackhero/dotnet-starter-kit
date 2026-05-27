using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FSH.Framework.Persistence;

/// <summary>
/// Internal extension methods for Entity Framework ModelBuilder configuration.
/// </summary>
internal static class ModelBuilderExtensions
{
    /// <summary>
    /// Registers a named global query filter on every entity that implements
    /// <typeparamref name="TInterface"/>. Named filters compose with anonymous
    /// filters (e.g. Finbuckle's tenant filter) and other named filters via
    /// AND at query time. To bypass only this filter at a specific call site,
    /// use <c>queryable.IgnoreQueryFilters([filterName])</c> — anonymous and
    /// other named filters remain in force.
    /// </summary>
    /// <typeparam name="TInterface">The interface type to filter entities by.</typeparam>
    /// <param name="modelBuilder">The ModelBuilder instance to configure.</param>
    /// <param name="filterName">A stable name for the filter (see <see cref="QueryFilters"/>).</param>
    /// <param name="filter">The filter expression to apply to all matching entities.</param>
    /// <returns>The ModelBuilder for method chaining.</returns>
    public static ModelBuilder AppendGlobalQueryFilter<TInterface>(
        this ModelBuilder modelBuilder,
        string filterName,
        Expression<Func<TInterface, bool>> filter)
    {
        var entities = modelBuilder.Model.GetEntityTypes()
            .Where(e => e.BaseType is null && e.ClrType.GetInterface(typeof(TInterface).Name) is not null)
            .Select(e => e.ClrType);

        foreach (var entity in entities)
        {
            var parameterType = Expression.Parameter(modelBuilder.Entity(entity).Metadata.ClrType);
            var filterBody = ReplacingExpressionVisitor.Replace(filter.Parameters.Single(), parameterType, filter.Body);
            modelBuilder.Entity(entity).HasQueryFilter(filterName, Expression.Lambda(filterBody, parameterType));
        }

        return modelBuilder;
    }
}

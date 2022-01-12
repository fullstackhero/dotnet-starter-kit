using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using FSH.WebAPI.Application.Common.Persistence;
using FSH.WebAPI.Domain.Common.Contracts;
using FSH.WebAPI.Infrastructure.Persistence.Context;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebAPI.Infrastructure.Persistence.Repository;

// Inherit from Ardalis.Specification RepositoryBase<T>
public class ApplicationDbRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public ApplicationDbRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    // AnyAsync is in the pipeline in Ardalis.Specifications... so this is temporary...
    public virtual async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken) =>
        await ApplySpecification(specification, true)
            .AnyAsync(cancellationToken);

    // We override the default behavior when mapping to a dto.
    // We're using Mapster's ProjectToType here to immediately map the result from the database.
    protected override IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification) =>
        ApplySpecification(specification, false)
            .ProjectToType<TResult>();
}
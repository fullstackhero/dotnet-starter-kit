using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Infrastructure.Persistence.Context;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Persistence.Repository;

// Inherited from Ardalis.Specification's RepositoryBase<T>
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
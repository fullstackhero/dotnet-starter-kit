using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Persistence.Repository;

// inherit from Ardalis.Specification type
public class ApplicationDbRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public ApplicationDbRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    // AnyAsync is in the pipeline in Ardalis.Specifications... so this is temporary...
    public virtual async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken)
    {
        return await ApplySpecification(specification, true).AnyAsync(cancellationToken);
    }
}
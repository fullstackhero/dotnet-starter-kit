using Ardalis.Specification.EntityFrameworkCore;
using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Infrastructure.Persistence.Context;

namespace DN.WebApi.Infrastructure.Persistence.Repository;

// inherit from Ardalis.Specification type
public class ApplicationDbRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public ApplicationDbRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }
}
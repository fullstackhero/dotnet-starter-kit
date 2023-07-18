using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using FL_CRMS_ERP_WEBAPI.Application.Common.Persistence;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Contracts;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Context;
using Mapster;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Repository;

// Inherited from Ardalis.Specification's RepositoryBase<T>
public class ApplicationDbRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public ApplicationDbRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    // We override the default behavior when mapping to a dto.
    // We're using Mapster's ProjectToType here to immediately map the result from the database.
    // This is only done when no Selector is defined, so regular specifications with a selector also still work.
    protected override IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification) =>
        specification.Selector is not null
            ? base.ApplySpecification(specification)
            : ApplySpecification(specification, false)
                .ProjectToType<TResult>();
}
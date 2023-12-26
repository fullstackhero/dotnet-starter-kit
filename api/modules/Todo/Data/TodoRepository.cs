using Ardalis.Specification.EntityFrameworkCore;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Framework.Core.Persistence;

namespace FSH.WebApi.Todo.Data;
internal class TodoRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public TodoRepository(TodoDbContext context)
        : base(context)
    {
    }
}

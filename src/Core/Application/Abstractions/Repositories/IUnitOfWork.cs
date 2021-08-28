using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Application.Abstractions.Repositories
{
    public interface IUnitOfWork<TId> : IDisposable
    {
    }
}
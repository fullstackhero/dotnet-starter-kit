using DN.WebApi.Domain.Entities.Catalog;

namespace DN.WebApi.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<Product> GetById(Guid id);
    }
}
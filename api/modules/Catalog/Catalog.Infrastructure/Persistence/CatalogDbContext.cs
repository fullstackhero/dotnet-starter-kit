using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Catalog.Infrastructure.Persistence;
internal sealed class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions options) : base(options)
    {
    }
}

using DN.WebApi.Application.Abstractions.Contexts;
using DN.WebApi.Application.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Persistence
{
    public class ApplicationDbContext : BaseDbContext,IApplicationDbContext
    {
        private readonly PersistenceConfiguration _persistenceConfig;
        public ApplicationDbContext(DbContextOptions options, IOptions<PersistenceConfiguration> persistenceConfig) : base(options,persistenceConfig)
        {
            _persistenceConfig = persistenceConfig.Value;
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
using DN.WebApi.Application.Abstractions.Database;
using DN.WebApi.Application.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Persistence
{
    public class ApplicationDbContext : BaseDbContext, IApplicationDbContext
    {
        private readonly DbSettings _dbSettings;
        public ApplicationDbContext(DbContextOptions options, IOptions<DbSettings> dbSettings) : base(options, dbSettings)
        {
            _dbSettings = dbSettings.Value;
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
using DN.WebApi.Application.Abstractions.Database;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Persistence
{
    public class ApplicationDbContext : BaseDbContext, IApplicationDbContext
    {
        private readonly ITenantService _tenantService;
        public ApplicationDbContext(DbContextOptions options, ITenantService tenantService) : base(options, tenantService)
        {
            _tenantService = tenantService;
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
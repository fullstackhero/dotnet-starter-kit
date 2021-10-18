using DN.WebApi.Domain.Entities.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DN.WebApi.Domain.Configuration
{
    public class TenantTypeConfiguration : AuditableEntityTypeConfiguration<Tenant>
    {
        public override string TableName => nameof(Tenant);

        public override void ConfigureEntity(EntityTypeBuilder<Tenant> builder)
        {
            builder.HasOne(d => d.ParentTenant)
                   .WithMany(p => p.SubTenants)
                   .HasForeignKey(d => d.ParentTenantId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

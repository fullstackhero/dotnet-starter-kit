using Finbuckle.MultiTenant;
using FSH.Framework.Auditing.Core.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Framework.Auditing.Infrastructure.Data;
public class AuditTrailConfig : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder
            .ToTable("AuditTrails", AuditingConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(a => a.Id);
    }
}

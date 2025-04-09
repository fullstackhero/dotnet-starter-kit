using Finbuckle.MultiTenant;
using FSH.Framework.Auditing.Core.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Framework.Auditing.Infrastructure.Data;
public class TrailConfig : IEntityTypeConfiguration<Trail>
{
    public void Configure(EntityTypeBuilder<Trail> builder)
    {
        builder
            .ToTable("Trails", AuditingConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(a => a.Id);
    }
}

using Finbuckle.MultiTenant;
using FSH.Framework.Auditing.Contracts;
using FSH.Framework.Auditing.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Framework.Auditing.Data;
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
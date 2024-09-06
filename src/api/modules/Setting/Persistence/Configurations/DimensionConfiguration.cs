using Finbuckle.MultiTenant;
using FSH.Starter.WebApi.Setting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Starter.WebApi.Setting.Persistence.Configurations;
internal sealed class DimensionConfiguration : IEntityTypeConfiguration<Dimension>
{
    public void Configure(EntityTypeBuilder<Dimension> builder)
    {
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100);
        builder.Property(x => x.Name).HasMaxLength(100);
    }
}

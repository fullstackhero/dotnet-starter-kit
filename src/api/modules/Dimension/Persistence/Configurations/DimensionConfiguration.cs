using Finbuckle.MultiTenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Starter.WebApi.Setting.Dimension.Persistence.Configurations;
internal sealed class DimensionConfiguration : IEntityTypeConfiguration<Dimension.Domain.Dimension>
{
    public void Configure(EntityTypeBuilder<Dimension.Domain.Dimension> builder)
    {
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100);
        builder.Property(x => x.Name).HasMaxLength(100);
    }
}

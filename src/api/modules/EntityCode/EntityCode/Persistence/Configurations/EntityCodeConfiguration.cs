using Finbuckle.MultiTenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Starter.WebApi.Setting.EntityCode.Persistence.Configurations;
internal sealed class EntityCodeConfiguration : IEntityTypeConfiguration<global::EntityCode>
{
    public void Configure(EntityTypeBuilder<global::EntityCode> builder)
    {
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100);
        builder.Property(x => x.Name).HasMaxLength(100);
    }
}

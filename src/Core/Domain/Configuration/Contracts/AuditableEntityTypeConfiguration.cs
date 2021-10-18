using DN.WebApi.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DN.WebApi.Domain.Configuration
{
    public abstract class AuditableEntityTypeConfiguration<TEntity> : BaseEntityTypeConfiguration<TEntity>
        where TEntity : AuditableEntity
    {
        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            ConfigureEntity(builder);

            builder.Property(p => p.IsModified);

            builder.Property(p => p.IsDeleted);

            builder.Property(p => p.CreatedBy)
                   .HasMaxLength(36);

            builder.Property(p => p.CreatedOn)
                   .ValueGeneratedOnAdd();

            base.Configure(builder);
        }
    }
}

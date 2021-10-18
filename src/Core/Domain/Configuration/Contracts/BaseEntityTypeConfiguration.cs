using DN.WebApi.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DN.WebApi.Domain.Configuration
{
    public abstract class BaseEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : BaseEntity
    {
        public abstract string TableName { get; }

        public abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);

        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            ConfigureEntity(builder);

            builder.HasKey(e => e.Id);

            builder.HasIndex(e => new { e.Referral })
                   .IsUnique();

            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            builder.Property(p => p.Referral)
                   .IsRequired()
                   .HasMaxLength(36);

            builder.Property(p => p.ConcurrencyStamp)
                   .IsConcurrencyToken()
                   .ValueGeneratedOnAddOrUpdate();
        }
    }
}

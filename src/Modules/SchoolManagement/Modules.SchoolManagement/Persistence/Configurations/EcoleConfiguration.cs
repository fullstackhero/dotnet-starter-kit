using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.SchoolManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.SchoolManagement.Persistence.Configurations;

public class EcoleConfiguration : IEntityTypeConfiguration<Ecole>
{
    public void Configure(EntityTypeBuilder<Ecole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Ecoles", "school");
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nom).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CodeEcole).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Adresse).HasMaxLength(500);
        builder.Property(x => x.Telephone).HasMaxLength(20);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Region).HasMaxLength(100);
        builder.Property(x => x.Ville).HasMaxLength(100);
        builder.HasIndex(x => x.CodeEcole).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}

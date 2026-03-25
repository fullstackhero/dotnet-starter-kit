using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.SchoolManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.SchoolManagement.Persistence.Configurations;

public class ClasseConfiguration : IEntityTypeConfiguration<Classe>
{
    public void Configure(EntityTypeBuilder<Classe> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Classes", "school");
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nom).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Niveau).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Capacite).IsRequired();
        builder.HasOne(x => x.Ecole)
            .WithMany()
            .HasForeignKey(x => x.EcoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AnneeScolaire)
            .WithMany()
            .HasForeignKey(x => x.AnneeScolaireId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EcoleId);
        builder.HasIndex(x => x.AnneeScolaireId);
    }
}

using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.SchoolManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.SchoolManagement.Persistence.Configurations;

public class AnneeScolaireConfiguration : IEntityTypeConfiguration<AnneeScolaire>
{
    public void Configure(EntityTypeBuilder<AnneeScolaire> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("AnneeScolaires", "school");
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Libelle).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.EstActive);
        builder.HasIndex(x => x.TenantId);
    }
}

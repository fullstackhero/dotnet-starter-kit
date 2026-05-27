using FSH.Modules.Tickets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Tickets.Data.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Tickets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number).IsRequired().HasMaxLength(32);
        // Unique per tenant — Finbuckle adds a TenantId to the row, so the
        // index ends up being effectively (TenantId, Number). Filter on
        // IsDeleted so soft-deleted ticket numbers don't conflict with new ones.
        builder.HasIndex(x => x.Number).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Property(x => x.Title).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Description).HasMaxLength(4096);
        builder.Property(x => x.ResolutionNote).HasMaxLength(4096);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AssignedToUserId);
        builder.HasIndex(x => x.ReporterUserId);
        builder.HasIndex(x => x.IsDeleted);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.HasMany(x => x.Comments)
            .WithOne()
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}

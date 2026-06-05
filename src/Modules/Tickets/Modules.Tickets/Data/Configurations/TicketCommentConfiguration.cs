using FSH.Modules.Tickets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Tickets.Data.Configurations;

public sealed class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TicketComments");
        builder.HasKey(x => x.Id);

        // Id is app-assigned (Guid.CreateVersion7) and comments attach only via the Ticket aggregate's nav
        // collection. Without ValueGeneratedNever, EF tracks the populated Guid as Modified → UPDATE-0-rows.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Body).IsRequired().HasMaxLength(8192);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.TicketId);
        builder.HasIndex(x => x.IsDeleted);
        builder.Ignore(x => x.DomainEvents);
    }
}

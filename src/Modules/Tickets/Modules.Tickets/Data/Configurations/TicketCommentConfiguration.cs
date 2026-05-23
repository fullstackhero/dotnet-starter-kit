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

        // TicketComment.Id is assigned in the domain factory via Guid.CreateVersion7().
        // TicketComments are only ever attached through the Ticket aggregate's nav
        // collection (ticket.AddComment(...)), never via dbContext.Set<TicketComment>().Add().
        // Without ValueGeneratedNever(), EF's default ValueGeneratedOnAdd treats the
        // already-populated Guid as "already persisted" during DetectChanges, tracks the
        // new comment as Modified, and SaveChanges emits an UPDATE that affects 0 rows →
        // DbUpdateConcurrencyException. ValueGeneratedNever() makes EF treat it as Added.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Body).IsRequired().HasMaxLength(8192);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.TicketId);
        builder.HasIndex(x => x.IsDeleted);
        builder.Ignore(x => x.DomainEvents);
    }
}

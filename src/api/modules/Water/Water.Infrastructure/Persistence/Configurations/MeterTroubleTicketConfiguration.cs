using Finbuckle.MultiTenant;
using FSH.Starter.WebApi.Water.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Starter.WebApi.Water.Infrastructure.Persistence.Configurations;

internal sealed class MeterTroubleTicketConfiguration : IEntityTypeConfiguration<MeterTroubleTicket>
{
    public void Configure(EntityTypeBuilder<MeterTroubleTicket> builder)
    {
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IssueDescription).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(1000);
        builder.HasOne(x => x.Meter)
            .WithMany()
            .HasForeignKey(x => x.MeterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Auditing.Persistence;

public class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("AuditRecords", "audit");
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasConversion<int>();
        builder.Property(x => x.Severity).HasConversion<byte>();
        builder.Property(x => x.Tags).HasConversion<long>();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb");

        // Individual indexes for common filter columns
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.Severity);
        builder.HasIndex(x => x.Source);
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.TraceId);
        builder.HasIndex(x => x.Tags);

        // Composite index for time-based queries (most common pattern)
        builder.HasIndex(x => new { x.TenantId, x.OccurredAtUtc })
            .IsDescending(false, true);

        // Composite index for user audit tracking
        builder.HasIndex(x => new { x.UserId, x.OccurredAtUtc })
            .IsDescending(false, true);

        // GIN index for JSONB full-text search (PostgreSQL specific)
        builder.HasIndex(x => x.PayloadJson)
            .HasMethod("gin")
            .HasAnnotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });
    }
}
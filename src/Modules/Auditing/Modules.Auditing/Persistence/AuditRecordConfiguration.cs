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

        // Hot-path index: default audits list filters on TenantId (Finbuckle) and orders by OccurredAtUtc DESC.
        // A composite over both lets PostgreSQL serve the paged top-N from an index-only walk.
        builder.HasIndex(x => new { x.TenantId, x.OccurredAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_AuditRecords_Tenant_OccurredAt");

        // Common dashboard slice: EventType within a tenant, ordered by time. Beats the
        // (TenantId, OccurredAtUtc) index when EventType is selective (e.g. only Security events).
        builder.HasIndex(x => new { x.TenantId, x.EventType, x.OccurredAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_AuditRecords_Tenant_EventType_OccurredAt");

        // Trace/correlation lookups ("everything tied to this request"). Both columns are sparse,
        // so single-column indexes are cheap and frequently selective.
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_AuditRecords_CorrelationId");
        builder.HasIndex(x => x.TraceId)
            .HasDatabaseName("IX_AuditRecords_TraceId");

        // ILIKE search on Source / UserName: pg_trgm GIN indexes turn `%term%` from a seq scan into a probe.
        // (pg_trgm extension is created at the context level.)
        builder.HasIndex(x => x.Source)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("IX_AuditRecords_Source_trgm");
        builder.HasIndex(x => x.UserName)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("IX_AuditRecords_UserName_trgm");

        // GIN over jsonb via jsonb_path_ops: supports containment (@>, ?) at far less disk than default jsonb_ops.
        // ILIKE on raw JSON text still seq-scans — extract indexed columns (Source, UserName) or denormalize for that.
        builder.HasIndex(x => x.PayloadJson)
            .HasMethod("gin")
            .HasOperators("jsonb_path_ops")
            .HasDatabaseName("IX_AuditRecords_PayloadJson_gin");
    }
}

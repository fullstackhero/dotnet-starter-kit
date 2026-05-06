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

        // ── Hot-path indexes ─────────────────────────────────────────────
        // The default audits list query filters on TenantId (via Finbuckle)
        // and orders by OccurredAtUtc DESC. A composite index that matches
        // both predicates lets PostgreSQL serve the paged top-N from an
        // index-only walk.
        builder.HasIndex(x => new { x.TenantId, x.OccurredAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_AuditRecords_Tenant_OccurredAt");

        // Common dashboard slice: filter by EventType inside a tenant, ordered
        // by time. Beats falling back to the (TenantId, OccurredAtUtc) index
        // when EventType is selective (e.g. only Security events).
        builder.HasIndex(x => new { x.TenantId, x.EventType, x.OccurredAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_AuditRecords_Tenant_EventType_OccurredAt");

        // Trace/correlation lookups for "show me everything tied to this
        // request". Both columns are sparse so a single-column index is
        // cheap and frequently selective.
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_AuditRecords_CorrelationId");
        builder.HasIndex(x => x.TraceId)
            .HasDatabaseName("IX_AuditRecords_TraceId");

        // Free-text-ish search on Source / UserName via ILIKE. pg_trgm GIN
        // indexes turn `%term%` from a sequential scan into an index probe.
        // The pg_trgm extension is created at the context level.
        builder.HasIndex(x => x.Source)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("IX_AuditRecords_Source_trgm");
        builder.HasIndex(x => x.UserName)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("IX_AuditRecords_UserName_trgm");

        // GIN over the jsonb payload using jsonb_path_ops — supports the
        // containment operators (@>, ?) at a fraction of the disk footprint
        // of the default jsonb_ops. ILIKE on the raw JSON text still does a
        // sequential scan; for that we recommend extracting indexed columns
        // (Source, UserName) or denormalizing search-relevant fields.
        builder.HasIndex(x => x.PayloadJson)
            .HasMethod("gin")
            .HasOperators("jsonb_path_ops")
            .HasDatabaseName("IX_AuditRecords_PayloadJson_gin");
    }
}

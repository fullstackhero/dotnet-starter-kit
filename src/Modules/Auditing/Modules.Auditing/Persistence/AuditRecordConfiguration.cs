using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Framework.Persistence.Providers;
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
        builder.Property(x => x.PayloadJson).HasJsonColumn();

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

        // Substring search on Source / UserName. On PostgreSQL these become pg_trgm GIN indexes,
        // which turn `%term%` from a seq scan into a probe (the pg_trgm extension is created at the
        // context level). SQL Server has no equivalent and both columns are nvarchar(max), so the
        // provider conventions pass drops these from the MSSQL model — see PortableIndexKind.
        builder.HasIndex(x => x.Source)
            .AsTrigramSearchIndex()
            .HasDatabaseName("IX_AuditRecords_Source_trgm");
        builder.HasIndex(x => x.UserName)
            .AsTrigramSearchIndex()
            .HasDatabaseName("IX_AuditRecords_UserName_trgm");

        // Containment search over the JSON payload: a jsonb_path_ops GIN index on PostgreSQL, a
        // CREATE JSON INDEX emitted by the migration on SQL Server (EF has no API for those, so the
        // conventions pass removes this index from the MSSQL model).
        // Substring search on raw JSON text still seq-scans on both providers — extract indexed
        // columns (Source, UserName) or denormalize for that.
        builder.HasIndex(x => x.PayloadJson)
            .AsJsonContainmentIndex()
            .HasDatabaseName("IX_AuditRecords_PayloadJson_gin");
    }
}

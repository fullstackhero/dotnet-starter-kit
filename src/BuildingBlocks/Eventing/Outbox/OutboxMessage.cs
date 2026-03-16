using FSH.Framework.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Framework.Eventing.Outbox;

/// <summary>
/// Outbox message entity used to persist integration events alongside domain changes.
/// </summary>
[IgnoreAuditTrail]
public class OutboxMessage
{
    public Guid Id { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string Type { get; set; } = default!;

    public string Payload { get; set; } = default!;

    public string? TenantId { get; set; }

    public string? CorrelationId { get; set; }

    public DateTime? ProcessedOnUtc { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }

    public bool IsDead { get; set; }
}

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    private readonly string _schema;

    public OutboxMessageConfiguration(string schema)
    {
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OutboxMessages", _schema);

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(o => o.Payload)
            .IsRequired();

        builder.Property(o => o.TenantId)
            .HasMaxLength(64);

        builder.Property(o => o.CorrelationId)
            .HasMaxLength(128);

        builder.Property(o => o.CreatedOnUtc)
            .IsRequired();
    }
}

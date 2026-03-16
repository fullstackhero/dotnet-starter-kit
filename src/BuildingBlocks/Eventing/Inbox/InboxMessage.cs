using FSH.Framework.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Framework.Eventing.Inbox;

/// <summary>
/// Inbox message to track processed integration events per handler for idempotent consumers.
/// </summary>
[IgnoreAuditTrail]
public class InboxMessage
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = default!;

    public string HandlerName { get; set; } = default!;

    public DateTime ProcessedOnUtc { get; set; }

    public string? TenantId { get; set; }
}

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    private readonly string _schema;

    public InboxMessageConfiguration(string schema)
    {
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("InboxMessages", _schema);

        builder.HasKey(i => new { i.Id, i.HandlerName });

        builder.Property(i => i.EventType)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(i => i.HandlerName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(i => i.TenantId)
            .HasMaxLength(64);
    }
}

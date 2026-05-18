using FSH.Framework.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Framework.Eventing.Inbox;

/// <summary>
/// Inbox message to track processed integration events per handler for idempotent consumers.
///
/// Implements <see cref="IGlobalEntity"/> to opt out of automatic tenant
/// filtering: inbox consumers run in background scopes and the
/// "already-processed" lookup must cross tenants. Per-row tenant
/// association is kept in the explicit nullable <see cref="TenantId"/> column.
/// </summary>
public class InboxMessage : IGlobalEntity
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
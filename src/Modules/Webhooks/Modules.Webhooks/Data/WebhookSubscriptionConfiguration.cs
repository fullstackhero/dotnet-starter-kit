using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.Webhooks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Webhooks.Data;

public sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Subscriptions", "webhooks");
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.EventsCsv).IsRequired().HasMaxLength(4096);
        builder.Property(x => x.ProtectedSecret).HasMaxLength(512);
        builder.HasIndex(x => x.IsActive);
        builder.Ignore(x => x.DomainEvents);
    }
}

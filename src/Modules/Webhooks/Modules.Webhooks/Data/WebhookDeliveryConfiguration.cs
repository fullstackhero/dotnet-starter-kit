using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.Webhooks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Webhooks.Data;

public sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Deliveries", "webhooks");
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(256);
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(4096);
        builder.HasIndex(x => x.SubscriptionId);
        builder.HasIndex(x => x.AttemptedAtUtc);
    }
}

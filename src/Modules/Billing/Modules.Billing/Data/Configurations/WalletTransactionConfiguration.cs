using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Billing.Data.Configurations;

public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("WalletTransactions");
        builder.HasKey(x => x.Id);
        // Child reached only via Wallet.Transactions nav — pin Id generation or EF marks Modified, not Added.
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.Kind).HasConversion<int>();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(256);
        builder.Property(x => x.ReferenceId).HasMaxLength(128);
        builder.HasIndex(x => new { x.WalletId, x.CreatedAtUtc });
        builder.HasIndex(x => x.TenantId);
    }
}

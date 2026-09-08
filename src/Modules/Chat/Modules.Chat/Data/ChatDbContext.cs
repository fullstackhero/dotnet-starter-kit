using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Chat.Data;

public sealed class ChatDbContext : BaseDbContext
{
    public const string Schema = "chat";

    public ChatDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<ChatDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<ChatChannel> Channels => Set<ChatChannel>();
    public DbSet<Message> Messages => Set<Message>();

    /// <summary>
    /// Shadow property holding the lexicographically-comparable form of <c>Message.Id</c>.
    /// SQL Server only.
    /// </summary>
    public const string MessageSortKey = "IdSort";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);

        if (Database.IsSqlServer())
        {
            ConfigureSqlServerMessageOrdering(modelBuilder);
        }

        // base.OnModelCreating runs LAST so BaseDbContext's auto-apply sees
        // fully-configured entities (including HasMany child types).
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Adds a sort key that restores chronological ordering of Guid v7 message ids on SQL Server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Message paging relies on "Guid v7 is monotonic, so Id DESC is time DESC". That holds on
    /// PostgreSQL, whose <c>uuid</c> compares in byte order. SQL Server's <c>uniqueidentifier</c>
    /// compares the <em>last</em> six bytes first, so a v7 id's leading timestamp is ignored and
    /// pages come back in the wrong order — silently, with no error. Casting to
    /// the canonical <c>char(36)</c> text form compares in display order and restores it.
    /// </para>
    /// <para>
    /// Text rather than <c>binary(16)</c> — both sort correctly, but EF Core cannot translate an
    /// ordering comparison on <c>byte[]</c>, so a binary key would force the ordering and the
    /// cursor predicate into raw SQL. The cost is a 36-byte index key instead of 16.
    /// </para>
    /// <para>
    /// Persisted and indexed so ordering and the <c>Before</c> cursor stay index-backed rather than
    /// sorting the channel's history on every page. SQL Server only — the PostgreSQL model and
    /// schema are untouched.
    /// </para>
    /// </remarks>
    private static void ConfigureSqlServerMessageOrdering(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(b =>
        {
            b.Property<string>(MessageSortKey)
                .HasColumnType("char(36)")
                .HasComputedColumnSql("CONVERT(char(36), [Id])", stored: true);

            b.HasIndex(nameof(Message.ChannelId), MessageSortKey)
                .IsDescending(false, true)
                .HasDatabaseName("IX_Messages_ChannelId_IdSort");

            b.HasIndex(nameof(Message.ParentMessageId), MessageSortKey)
                .IsDescending(false, true)
                .HasDatabaseName("IX_Messages_ParentMessageId_IdSort");
        });
    }
}

using FSH.Framework.Persistence.Providers;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;

namespace Framework.Tests.Persistence;

/// <summary>
/// Model-level proof that portable column and index intent resolves to the right provider SQL.
/// Pure model building — no database, no Docker.
/// </summary>
public class ProviderConventionsTests
{
    private sealed class Widget
    {
        public Guid Id { get; set; }
        public string Payload { get; set; } = default!;
        public string Body { get; set; } = default!;
        public string Config { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPinned { get; set; }
        public int Status { get; set; }
        public string? Slug { get; set; }
        public string? Source { get; set; }
    }

    private sealed class ProbeDbContext(DbContextOptions options, string provider) : DbContext(options)
    {
        public DbSet<Widget> Widgets => Set<Widget>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.AddHeroProviderConventions(provider);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Widget>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Payload).HasJsonColumn();
                b.Property(x => x.Config).HasJsonColumn().HasJsonDefaultEmptyObject();
                b.Property(x => x.Body).HasUnboundedTextColumn();
                b.Property(x => x.CreatedAtUtc).HasUtcNowDefault();

                b.HasIndex(x => x.Slug).IsUnique().HasNotDeletedFilter();
                b.HasIndex(x => x.IsPinned).HasBoolFilter("IsPinned", true);
                b.HasIndex(x => x.Status).HasEqualsFilter("Status", 3).HasNotDeletedFilter();
                b.HasIndex(x => x.DeletedAtUtc).HasNotNullFilter("DeletedAtUtc");

                b.HasIndex(x => x.Source).AsTrigramSearchIndex().HasDatabaseName("IX_Widget_Source_trgm");
                b.HasIndex(x => x.Payload).AsJsonContainmentIndex().HasDatabaseName("IX_Widget_Payload_gin");
            });
        }
    }

    private static IModel BuildModel(string provider)
    {
        var options = new DbContextOptionsBuilder();

        if (provider == DbProviders.MSSQL)
        {
            options.UseSqlServer("Server=probe;Database=probe;Trusted_Connection=True");
        }
        else
        {
            options.UseNpgsql("Host=probe;Database=probe;Username=probe;Password=probe");
        }

        using var context = new ProbeDbContext(options.Options, provider);
        return context.Model;
    }

    private static IEntityType WidgetEntity(string provider) =>
        BuildModel(provider).FindEntityType(typeof(Widget))!;

    private static string? ColumnType(string provider, string propertyName) =>
        WidgetEntity(provider).FindProperty(propertyName)!.GetColumnType();

    private static string? FilterFor(string provider, string propertyName) =>
        WidgetEntity(provider).GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == propertyName)
            .GetFilter();

    [Theory]
    [InlineData(DbProviders.PostgreSQL, "jsonb")]
    [InlineData(DbProviders.MSSQL, "json")]
    public void HasJsonColumn_Should_Map_To_The_Providers_Native_Json_Type(string provider, string expected)
        => ColumnType(provider, nameof(Widget.Payload)).ShouldBe(expected);

    [Theory]
    [InlineData(DbProviders.PostgreSQL, "text")]
    [InlineData(DbProviders.MSSQL, "nvarchar(max)")]
    public void HasUnboundedTextColumn_Should_Map_To_The_Providers_Text_Type(string provider, string expected)
        => ColumnType(provider, nameof(Widget.Body)).ShouldBe(expected);

    [Theory]
    [InlineData(DbProviders.PostgreSQL, "'{}'::jsonb")]
    [InlineData(DbProviders.MSSQL, "N'{}'")]
    public void HasJsonDefaultEmptyObject_Should_Use_Provider_Syntax(string provider, string expected)
        => WidgetEntity(provider).FindProperty(nameof(Widget.Config))!.GetDefaultValueSql().ShouldBe(expected);

    [Theory]
    [InlineData(DbProviders.PostgreSQL, "CURRENT_TIMESTAMP")]
    [InlineData(DbProviders.MSSQL, "SYSUTCDATETIME()")]
    public void HasUtcNowDefault_Should_Use_Provider_Function(string provider, string expected)
        => WidgetEntity(provider).FindProperty(nameof(Widget.CreatedAtUtc))!.GetDefaultValueSql().ShouldBe(expected);

    [Theory]
    [InlineData(DbProviders.PostgreSQL, "\"IsDeleted\" = FALSE")]
    [InlineData(DbProviders.MSSQL, "[IsDeleted] = 0")]
    public void HasNotDeletedFilter_Should_Quote_And_Spell_Booleans_Per_Provider(string provider, string expected)
        => FilterFor(provider, nameof(Widget.Slug)).ShouldBe(expected);

    [Theory]
    [InlineData(DbProviders.PostgreSQL, "\"IsPinned\" = true")]
    [InlineData(DbProviders.MSSQL, "[IsPinned] = 1")]
    public void HasBoolFilter_Should_Preserve_The_Shipped_Postgres_Spelling(string provider, string expected)
        => FilterFor(provider, nameof(Widget.IsPinned)).ShouldBe(expected);

    [Theory]
    [InlineData(DbProviders.PostgreSQL, "\"Status\" = 3 AND \"IsDeleted\" = FALSE")]
    [InlineData(DbProviders.MSSQL, "[Status] = 3 AND [IsDeleted] = 0")]
    public void Filter_Terms_Should_Be_ANDed_In_Declaration_Order(string provider, string expected)
        => FilterFor(provider, nameof(Widget.Status)).ShouldBe(expected);

    [Theory]
    [InlineData(DbProviders.PostgreSQL, "\"DeletedAtUtc\" IS NOT NULL")]
    [InlineData(DbProviders.MSSQL, "[DeletedAtUtc] IS NOT NULL")]
    public void HasNotNullFilter_Should_Render_On_Both_Providers(string provider, string expected)
        => FilterFor(provider, nameof(Widget.DeletedAtUtc)).ShouldBe(expected);

    [Theory]
    [InlineData("IX_Widget_Source_trgm")]
    [InlineData("IX_Widget_Payload_gin")]
    public void Search_Indexes_Should_Become_Gin_Indexes_On_Postgres(string indexName)
    {
        // Only the index method is observable on the finalized read-model; Npgsql does not surface
        // the operator class (gin_trgm_ops / jsonb_path_ops) as a readable annotation there. That
        // half is covered by the migrations snapshot instead — see the has-pending-model-changes
        // gate, which fails if either annotation stops being emitted.
        IIndex index = WidgetEntity(DbProviders.PostgreSQL).GetIndexes()
            .Single(i => i.GetDatabaseName() == indexName);

        index.FindAnnotation("Npgsql:IndexMethod")!.Value.ShouldBe("gin");
    }

    [Fact]
    public void Search_Indexes_Should_Be_Removed_On_SqlServer()
    {
        // Neither shape is a legal regular index on SQL Server: one targets a `json` column, the
        // other an nvarchar(max). Leaving them in the model would make the migration fail.
        var names = WidgetEntity(DbProviders.MSSQL).GetIndexes().Select(i => i.GetDatabaseName()).ToList();

        names.ShouldNotContain("IX_Widget_Source_trgm");
        names.ShouldNotContain("IX_Widget_Payload_gin");
    }

    [Fact]
    public void DateTime_Properties_Should_Read_Back_As_Utc_On_SqlServer_Only()
    {
        // datetime2 carries no kind, so without a converter every timestamp the API serializes
        // would lose its trailing Z. timestamptz already round-trips as Utc.
        WidgetEntity(DbProviders.MSSQL).FindProperty(nameof(Widget.CreatedAtUtc))!
            .GetValueConverter().ShouldNotBeNull();

        WidgetEntity(DbProviders.PostgreSQL).FindProperty(nameof(Widget.CreatedAtUtc))!
            .GetValueConverter().ShouldBeNull();
    }

    [Fact]
    public void Intent_Annotations_Should_Not_Survive_Into_The_Model()
    {
        // A leftover Fsh:* annotation reaches the migrations snapshot, where EF must emit it as a
        // C# literal. Anything non-primitive fails the scaffolder outright.
        foreach (string provider in new[] { DbProviders.PostgreSQL, DbProviders.MSSQL })
        {
            IEntityType entity = WidgetEntity(provider);

            entity.GetProperties()
                .SelectMany(p => p.GetAnnotations())
                .Select(a => a.Name)
                .ShouldNotContain(n => n.StartsWith("Fsh:", StringComparison.Ordinal));

            entity.GetIndexes()
                .SelectMany(i => i.GetAnnotations())
                .Select(a => a.Name)
                .ShouldNotContain(n => n.StartsWith("Fsh:", StringComparison.Ordinal));
        }
    }
}

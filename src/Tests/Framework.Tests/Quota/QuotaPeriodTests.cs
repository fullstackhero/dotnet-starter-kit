using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StackExchange.Redis;

namespace Framework.Tests.Quota;

public sealed class QuotaPeriodTests
{
    #region InMemory

    [Fact]
    public async Task GetForPeriodAsync_Should_ReadRequestedMonth_When_CurrentMonthDiffers()
    {
        // Arrange
        var timeProvider = new AdjustableTimeProvider(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));
        var service = CreateInMemoryService(timeProvider);
        await service.RecordAsync("tenant-1", QuotaResource.ApiCalls, 37);
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));

        // Act
        var periodUsage = await service.GetForPeriodAsync("tenant-1", QuotaResource.ApiCalls, 2026, 10);
        var currentUsage = await service.GetCurrentAsync("tenant-1", QuotaResource.ApiCalls);

        // Assert
        periodUsage.ShouldBe(37);
        currentUsage.ShouldBe(0);
    }

    [Fact]
    public async Task GetForPeriodAsync_Should_ReturnZero_When_PeriodCounterDoesNotExist()
    {
        // Arrange
        var service = CreateInMemoryService(TimeProvider.System);

        // Act
        var result = await service.GetForPeriodAsync("tenant-1", QuotaResource.ApiCalls, 2026, 10);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task GetForPeriodAsync_Should_ReturnCurrentValue_ForNonPeriodicResource()
    {
        // Arrange
        var service = CreateInMemoryService(TimeProvider.System);
        await service.RecordAsync("tenant-1", QuotaResource.StorageBytes, 37);

        // Act
        var result = await service.GetForPeriodAsync("tenant-1", QuotaResource.StorageBytes, 2026, 10);

        // Assert
        result.ShouldBe(37);
    }

    [Theory]
    [InlineData(1999, 10)]
    [InlineData(2101, 10)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task GetForPeriodAsync_Should_RejectInvalidPeriod_ForAllImplementations(int year, int month)
    {
        // Arrange
        var service = CreateInMemoryService(TimeProvider.System);
        var (redisService, _) = CreateRedisService(DateTimeOffset.UtcNow);
        var noopService = new NoopQuotaService();

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await service.GetForPeriodAsync("tenant-1", QuotaResource.ApiCalls, year, month));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await redisService.GetForPeriodAsync("tenant-1", QuotaResource.ApiCalls, year, month));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await noopService.GetForPeriodAsync("tenant-1", QuotaResource.ApiCalls, year, month));
    }

    [Theory]
    [InlineData(2000, 1)]
    [InlineData(2100, 12)]
    public async Task GetForPeriodAsync_Should_AcceptBillingPeriodBoundaries_ForAllImplementations(int year, int month)
    {
        // Arrange
        var service = CreateInMemoryService(TimeProvider.System);
        var (redisService, _) = CreateRedisService(DateTimeOffset.UtcNow);
        var noopService = new NoopQuotaService();

        // Act & Assert
        (await service.GetForPeriodAsync("tenant-1", QuotaResource.ApiCalls, year, month)).ShouldBe(0);
        (await redisService.GetForPeriodAsync("tenant-1", QuotaResource.ApiCalls, year, month)).ShouldBe(0);
        (await noopService.GetForPeriodAsync("tenant-1", QuotaResource.ApiCalls, year, month)).ShouldBe(0);
    }

    #endregion

    #region Redis

    [Fact]
    public async Task GetForPeriodAsync_Should_ReadExplicitPeriodKey_When_CurrentMonthDiffers()
    {
        // Arrange
        var (service, database) = CreateRedisService(
            new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero),
            new QuotaOptions { KeyPrefix = "app:" });
        var expectedKey = (RedisKey)"app:quota:tenant-1:ApiCalls:202610";
        database.StringGetAsync(expectedKey, CommandFlags.None).Returns((RedisValue)37L);

        // Act
        var result = await service.GetForPeriodAsync("tenant-1", QuotaResource.ApiCalls, 2026, 10);

        // Assert
        result.ShouldBe(37);
        await database.Received(1).StringGetAsync(expectedKey, CommandFlags.None);
    }

    [Fact]
    public async Task GetForPeriodAsync_Should_ReturnZero_When_PeriodKeyIsMissing()
    {
        // Arrange
        var (service, database) = CreateRedisService(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));
        var expectedKey = (RedisKey)"quota:tenant-1:ApiCalls:202610";
        database.StringGetAsync(expectedKey, CommandFlags.None).Returns(RedisValue.Null);

        // Act
        var result = await service.GetForPeriodAsync("tenant-1", QuotaResource.ApiCalls, 2026, 10);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task GetForPeriodAsync_Should_ReadCurrentCounter_ForNonPeriodicResource()
    {
        // Arrange
        var (service, database) = CreateRedisService(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));
        var expectedKey = (RedisKey)"quota:tenant-1:StorageBytes";
        database.StringGetAsync(expectedKey, CommandFlags.None).Returns((RedisValue)37L);

        // Act
        var result = await service.GetForPeriodAsync("tenant-1", QuotaResource.StorageBytes, 2026, 10);

        // Assert
        result.ShouldBe(37);
        await database.Received(1).StringGetAsync(expectedKey, CommandFlags.None);
    }

    [Fact]
    public async Task RecordAsync_Should_RetainMonthlyKeyAfterPeriodEnd_When_Configured()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new QuotaOptions { PeriodCounterRetentionDays = 90 };
        var (service, database) = CreateRedisService(now, options);
        var expectedKey = (RedisKey)"quota:tenant-1:ApiCalls:202610";
        database.StringIncrementAsync(expectedKey, 1, CommandFlags.None).Returns(1L);
        var expectedExpiration = new DateTime(2027, 1, 30, 0, 0, 0, DateTimeKind.Utc);
        database.KeyExpireAsync(expectedKey, expectedExpiration, ExpireWhen.Always, CommandFlags.None).Returns(true);

        // Act
        var result = await service.RecordAsync("tenant-1", QuotaResource.ApiCalls, 1);

        // Assert
        result.ShouldBe(1);
        await database.Received(1).KeyExpireAsync(expectedKey, expectedExpiration, ExpireWhen.Always, CommandFlags.None);
    }

    [Fact]
    public async Task CheckAndRecordAsync_Should_RetainMonthlyKeyAfterPeriodEnd_When_Configured()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new QuotaOptions { PeriodCounterRetentionDays = 90 };
        options.Plans[options.DefaultPlan] = new Dictionary<QuotaResource, long>
        {
            [QuotaResource.ApiCalls] = 100
        };
        var (service, database) = CreateRedisService(now, options);
        var expectedKey = (RedisKey)"quota:tenant-1:ApiCalls:202610";
        database.StringIncrementAsync(expectedKey, 1, CommandFlags.None).Returns(1L);
        var expectedExpiration = new DateTime(2027, 1, 30, 0, 0, 0, DateTimeKind.Utc);
        database.KeyExpireAsync(expectedKey, expectedExpiration, ExpireWhen.Always, CommandFlags.None).Returns(true);

        // Act
        var result = await service.CheckAndRecordAsync("tenant-1", QuotaResource.ApiCalls, 1);

        // Assert
        result.Allowed.ShouldBeTrue();
        await database.Received(1).KeyExpireAsync(expectedKey, expectedExpiration, ExpireWhen.Always, CommandFlags.None);
    }

    #endregion

    private static InMemoryQuotaService CreateInMemoryService(TimeProvider timeProvider)
    {
        var options = new QuotaOptions();
        return new InMemoryQuotaService(new InMemoryQuotaStore(), options, new QuotaPlanResolver(options), [], timeProvider);
    }

    private static (RedisQuotaService Service, IDatabase Database) CreateRedisService(
        DateTimeOffset now,
        QuotaOptions? options = null)
    {
        var database = Substitute.For<IDatabase>();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        multiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        var quotaOptions = options ?? new QuotaOptions();
        var service = new RedisQuotaService(
            multiplexer,
            quotaOptions,
            new QuotaPlanResolver(quotaOptions),
            [],
            new AdjustableTimeProvider(now),
            NullLogger<RedisQuotaService>.Instance);
        return (service, database);
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
    }
}

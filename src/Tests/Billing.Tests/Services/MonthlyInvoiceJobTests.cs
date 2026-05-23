using FSH.Modules.Billing.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Billing.Tests.Services;

public sealed class MonthlyInvoiceJobTests
{
    private readonly IBillingService _billing = Substitute.For<IBillingService>();

    private MonthlyInvoiceJob CreateSut() => new(_billing, NullLogger<MonthlyInvoiceJob>.Instance);

    #region Happy Path

    [Fact]
    public async Task RunAsync_Should_Generate_For_Previous_Period()
    {
        var previous = DateTime.UtcNow.AddMonths(-1);
        _billing.GenerateInvoicesForAllTenantsAsync(previous.Year, previous.Month, Arg.Any<CancellationToken>())
            .Returns(3);
        var sut = CreateSut();

        await sut.RunAsync(CancellationToken.None);

        await _billing.Received(1)
            .GenerateInvoicesForAllTenantsAsync(previous.Year, previous.Month, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Should_Propagate_CancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        await sut.RunAsync(cts.Token);

        await _billing.Received(1)
            .GenerateInvoicesForAllTenantsAsync(Arg.Any<int>(), Arg.Any<int>(), cts.Token);
    }

    #endregion

    #region Exceptions

    [Fact]
    public async Task RunAsync_Should_Bubble_Up_Service_Failure()
    {
        _billing.GenerateInvoicesForAllTenantsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new InvalidOperationException("boom"));
        var sut = CreateSut();

        await Should.ThrowAsync<InvalidOperationException>(() => sut.RunAsync(CancellationToken.None));
    }

    #endregion
}

using DN.WebApi.Application.Catalog.Brands;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Domain.Catalog.Brands;
using DN.WebApi.Shared.DTOs.Notifications;
using Hangfire;
using Hangfire.Console.Extensions;
using Hangfire.Console.Progress;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Infrastructure.Catalog;

public class BrandGeneratorJob : IBrandGeneratorJob
{
    private readonly ILogger<BrandGeneratorJob> _logger;
    private readonly IRepositoryAsync _repository;
    private readonly IProgressBarFactory _progressBar;
    private readonly PerformingContext _performingContext;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUser _currentUser;
    private readonly IProgressBar _progress;

    public BrandGeneratorJob(
        ILogger<BrandGeneratorJob> logger,
        IRepositoryAsync repository,
        IProgressBarFactory progressBar,
        PerformingContext performingContext,
        INotificationService notificationService,
        ICurrentUser currentUser)
    {
        _logger = logger;
        _repository = repository;
        _progressBar = progressBar;
        _performingContext = performingContext;
        _notificationService = notificationService;
        _currentUser = currentUser;
        _progress = _progressBar.Create();
    }

    private async Task NotifyAsync(string message, int progress, CancellationToken cancellationToken)
    {
        _progress.SetValue(progress);
        await _notificationService.SendMessageToUserAsync(
            _currentUser.GetUserId().ToString(),
            new JobNotification()
            {
                JobId = _performingContext.BackgroundJob.Id,
                Message = message,
                Progress = progress
            },
            cancellationToken);
    }

    [Queue("notdefault")]
    public async Task GenerateAsync(int nSeed, CancellationToken cancellationToken)
    {
        await NotifyAsync("Your job processing has started", 0, cancellationToken);
        foreach (int index in Enumerable.Range(1, nSeed))
        {
            var brand = new Brand(name: $"Brand Random - {Guid.NewGuid()}", "Funny description");
            brand.DomainEvents.Add(new BrandCreatedEvent(brand));
            await _repository.CreateAsync(brand, cancellationToken);
            await NotifyAsync("Progress: ", nSeed > 0 ? (index * 100 / nSeed) : 0, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await NotifyAsync("Job successfully completed", 0, cancellationToken);
    }

    [Queue("notdefault")]
    [AutomaticRetry(Attempts = 5)]
    public async Task CleanAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Job with Id: {JobId}", _performingContext.BackgroundJob.Id);
        var items = await _repository.GetListAsync<Brand>(x => !string.IsNullOrEmpty(x.Name) && x.Name.Contains("Brand Random"), cancellationToken: cancellationToken);
        _logger.LogInformation("Brands Random: {BrandsCount} ", items.Count.ToString());

        foreach (var item in items)
        {
            item.DomainEvents.Add(new BrandDeletedEvent(item));
            await _repository.RemoveAsync(item, cancellationToken);
        }

        int rows = await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Rows affected: {rows} ", rows.ToString());
    }
}
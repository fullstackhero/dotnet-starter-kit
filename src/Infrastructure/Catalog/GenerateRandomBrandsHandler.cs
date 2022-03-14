using FSH.WebApi.Application.Catalog.Brands;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Shared.Notifications;
using Hangfire.Console.Extensions;
using Hangfire.Console.Progress;
using Hangfire.Server;
using MediatR;

namespace FSH.WebApi.Infrastructure.Catalog;

public class GenerateRandomBrandsHandler : IRequestHandler<GenerateRandomBrands>
{
    private readonly ISender _mediator;
    private readonly IProgressBarFactory _progressBar;
    private readonly PerformingContext _performingContext;
    private readonly INotificationSender _notifications;
    private readonly ICurrentUser _currentUser;
    private readonly IProgressBar _progress;

    public GenerateRandomBrandsHandler(
        ISender mediator,
        IProgressBarFactory progressBar,
        PerformingContext performingContext,
        INotificationSender notifications,
        ICurrentUser currentUser)
    {
        _mediator = mediator;
        _progressBar = progressBar;
        _performingContext = performingContext;
        _notifications = notifications;
        _currentUser = currentUser;
        _progress = _progressBar.Create();
    }

    public async Task<Unit> Handle(GenerateRandomBrands request, CancellationToken cancellationToken)
    {
        await NotifyAsync("Your job processing has started", 0, cancellationToken);

        foreach (int index in Enumerable.Range(1, request.NSeed))
        {
            await _mediator.Send(
                new CreateBrandRequest
                {
                    Name = $"Brand Random - {Guid.NewGuid()}",
                    Description = "Funny description"
                },
                cancellationToken);

            await NotifyAsync("Progress: ", request.NSeed > 0 ? (index * 100 / request.NSeed) : 0, cancellationToken);
        }

        await NotifyAsync("Job successfully completed", 0, cancellationToken);

        return Unit.Value;
    }

    private async Task NotifyAsync(string message, int progress, CancellationToken cancellationToken)
    {
        _progress.SetValue(progress);
        await _notifications.SendToUserAsync(
            new JobNotification()
            {
                JobId = _performingContext.BackgroundJob.Id,
                Message = message,
                Progress = progress
            },
            _currentUser.GetUserId().ToString(),
            cancellationToken);
    }
}
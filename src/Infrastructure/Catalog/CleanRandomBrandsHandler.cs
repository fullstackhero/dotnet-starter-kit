using FSH.WebApi.Application.Catalog.Brands;
using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Domain.Catalog;
using Hangfire.Server;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.Catalog;

public class CleanRandomBrandsHandler : IRequestHandler<DeleteRandomBrands>
{
    private readonly ILogger<CleanRandomBrandsHandler> _logger;

    public CleanRandomBrandsHandler(ILogger<CleanRandomBrandsHandler> logger, ISender mediator, IReadRepository<Brand> repository, PerformingContext performingContext)
    {
        _logger = logger;
        _mediator = mediator;
        _repository = repository;
        _performingContext = performingContext;
    }

    private readonly ISender _mediator;
    private readonly IReadRepository<Brand> _repository;
    private readonly PerformingContext _performingContext;

    public async Task<Unit> Handle(DeleteRandomBrands request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Job with Id: {jobId}", _performingContext.BackgroundJob.Id);

        var items = await _repository.ListAsync(new RandomBrandsSpec(), cancellationToken);

        _logger.LogInformation("Brands Random: {brandsCount} ", items.Count.ToString());

        foreach (var item in items)
        {
            await _mediator.Send(new DeleteBrandRequest(item.Id), cancellationToken);
        }

        _logger.LogInformation("All random brands deleted.");

        return Unit.Value;
    }
}
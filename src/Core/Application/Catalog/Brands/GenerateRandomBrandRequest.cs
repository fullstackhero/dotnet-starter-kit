using DN.WebApi.Application.Common.Interfaces;
using MediatR;

namespace DN.WebApi.Application.Catalog.Brands;

public class GenerateRandomBrandRequest : IRequest<string>
{
    public int NSeed { get; set; }
}

public class GenerateRandomBrandRequestHandler : IRequestHandler<GenerateRandomBrandRequest, string>
{
    private readonly IJobService _jobService;

    public GenerateRandomBrandRequestHandler(IJobService jobService) => _jobService = jobService;

    public Task<string> Handle(GenerateRandomBrandRequest request, CancellationToken cancellationToken)
    {
        string jobId = _jobService.Enqueue<IBrandGeneratorJob>(x => x.GenerateAsync(request.NSeed));
        return Task.FromResult(jobId);
    }
}
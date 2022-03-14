namespace FSH.WebApi.Application.Catalog.Brands;

public class GenerateRandomBrandRequest : IRequest<string>
{
    public int NSeed { get; set; }
}

// The request for which the handler runs in hangfire using _jobService.Enqueue
// The handler is in the infrastructure project, but could as well be here if it doesn't use hangfire-specific stuff.
public class GenerateRandomBrands : IRequest
{
    public int NSeed { get; set; }
}

public class GenerateRandomBrandRequestHandler : IRequestHandler<GenerateRandomBrandRequest, string>
{
    private readonly IJobService _jobService;

    public GenerateRandomBrandRequestHandler(IJobService jobService) => _jobService = jobService;

    public Task<string> Handle(GenerateRandomBrandRequest request, CancellationToken cancellationToken)
    {
        // The classic way
        // string jobId = _jobService.Enqueue<IBrandGeneratorJob>(x => x.GenerateAsync(request.NSeed, default));

        // The new way
        string jobId = _jobService.Enqueue(new GenerateRandomBrands { NSeed = request.NSeed });

        return Task.FromResult(jobId);
    }
}
namespace FSH.WebApi.Application.Catalog.Brands;

public class DeleteRandomBrandRequest : IRequest<string>
{
}

public class DeleteRandomBrands : IRequest
{
}

public class DeleteRandomBrandRequestHandler : IRequestHandler<DeleteRandomBrandRequest, string>
{
    private readonly IJobService _jobService;

    public DeleteRandomBrandRequestHandler(IJobService jobService) => _jobService = jobService;

    public Task<string> Handle(DeleteRandomBrandRequest request, CancellationToken cancellationToken)
    {
        // string jobId = _jobService.Schedule<IBrandGeneratorJob>(x => x.CleanAsync(default), TimeSpan.FromSeconds(5));
        string jobId = "testje";

        // Register as a recurring job running every minute
        _jobService.AddOrUpdate(jobId, new DeleteRandomBrands(), "* * * * *");

        return Task.FromResult(jobId);
    }
}
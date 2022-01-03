using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Catalog.Interfaces;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Host.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

[ApiConventionType(typeof(FSHApiConventions))]
public class GenerateRandom : EndpointBaseAsync
    .WithRequest<GenerateRandomBrandRequest>
    .WithResult<Result<string>>
{
    private readonly IJobService _jobService;

    public GenerateRandom(IJobService jobService) =>
        _jobService = jobService;

    [HttpPost("generate-random")]
    public override Task<Result<string>> HandleAsync(GenerateRandomBrandRequest request, CancellationToken cancellationToken = default)
    {
        string jobId = _jobService.Enqueue<IBrandGeneratorJob>(x => x.GenerateAsync(request.NSeed));
        return Result<string>.SuccessAsync(jobId);
    }
}
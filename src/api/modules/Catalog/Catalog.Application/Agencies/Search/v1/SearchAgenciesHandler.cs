using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Application.Agencies.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Catalog.Application.Agencies.Search.v1;
public sealed class SearchAgenciesHandler(
    [FromKeyedServices("catalog:agencies")] IReadRepository<Agency> repository)
    : IRequestHandler<SearchAgenciesCommand, PagedList<AgencyResponse>>
{
    public async Task<PagedList<AgencyResponse>> Handle(SearchAgenciesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new SearchAgencySpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<AgencyResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}
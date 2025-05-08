using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Application.Properties.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Properties.Search.v1;

public sealed class SearchPropertiesHandler(
    IReadRepository<Property> repository)
    : IRequestHandler<SearchPropertiesCommand, PagedList<PropertyResponse>>
{
    public async Task<PagedList<PropertyResponse>> Handle(SearchPropertiesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new SearchPropertySpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<PropertyResponse>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
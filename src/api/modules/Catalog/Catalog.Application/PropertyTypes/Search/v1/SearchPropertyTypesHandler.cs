using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Search.v1;

public sealed class SearchPropertyTypesHandler(
    IReadRepository<PropertyType> repository)
    : IRequestHandler<SearchPropertyTypesCommand, PagedList<PropertyTypeResponse>>
{
    public async Task<PagedList<PropertyTypeResponse>> Handle(SearchPropertyTypesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new SearchPropertyTypeSpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<PropertyTypeResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}

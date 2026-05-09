using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Application.Tariffs.Get.v1;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Search.v1;

public sealed class SearchTariffsHandler(
    [FromKeyedServices("water:tariffs")] IReadRepository<Tariff> repository)
    : IRequestHandler<SearchTariffsCommand, PagedList<TariffResponse>>
{
    public async Task<PagedList<TariffResponse>> Handle(SearchTariffsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = new SearchTariffSpecs(request);
        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);
        return new PagedList<TariffResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}

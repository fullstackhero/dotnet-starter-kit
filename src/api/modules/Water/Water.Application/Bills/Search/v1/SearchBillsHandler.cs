using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Application.Bills.Get.v1;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.Bills.Search.v1;

public sealed class SearchBillsHandler(
    [FromKeyedServices("water:bills")] IReadRepository<Bill> repository)
    : IRequestHandler<SearchBillsCommand, PagedList<BillResponse>>
{
    public async Task<PagedList<BillResponse>> Handle(SearchBillsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = new SearchBillSpecs(request);
        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);
        return new PagedList<BillResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}

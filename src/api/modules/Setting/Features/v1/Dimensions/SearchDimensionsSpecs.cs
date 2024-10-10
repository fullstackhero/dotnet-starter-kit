using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Setting.Domain;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public sealed class SearchDimensionsSpecs : EntitiesByPaginationFilterSpec<Dimension, DimensionDto>
{
    public SearchDimensionsSpecs(SearchDimensionsRequest request)
        : base(request) =>
            Query
                .Where(e => e.IsActive.Equals(request.IsActive!), request.IsActive.HasValue)
                .Where(e => string.IsNullOrEmpty(request.Type) || e.Type.Equals(request.Type, StringComparison.Ordinal))
                .Where(e => e.FatherId.Equals(request.FatherId!.Value), request.FatherId.HasValue)
                    .OrderBy(e => e.Order, !request.HasOrderBy());
}

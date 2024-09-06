using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
public record GetDimensionListRequest(PaginationFilter filter) : IRequest<PagedList<DimensionDto>>;

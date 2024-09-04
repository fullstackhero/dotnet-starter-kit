using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;
public record GetDimensionListRequest(PaginationFilter filter) : IRequest<PagedList<DimensionDto>>;

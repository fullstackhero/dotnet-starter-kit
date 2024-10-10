using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public record GetEntityCodeListRequest(PaginationFilter filter) : IRequest<PagedList<EntityCodeDto>>;

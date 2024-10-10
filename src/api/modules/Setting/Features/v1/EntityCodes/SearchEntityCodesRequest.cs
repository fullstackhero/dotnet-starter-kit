using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public class SearchEntityCodesRequest : PaginationFilter, IRequest<PagedList<EntityCodeDto>>
{
    public CodeType? Type { get; set; }
}

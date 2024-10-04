using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public class GetEntityCodesRequest : BaseFilter, IRequest<List<EntityCodeDto>>
{
    public CodeType? Type { get; set; }
}

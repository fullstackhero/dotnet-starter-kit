using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public class ExportEntityCodesRequest : BaseFilter, IRequest<byte[]>
{
    public CodeType? Type { get; set; }
}

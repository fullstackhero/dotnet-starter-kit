using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public class ExportDimensionsRequest : BaseFilter, IRequest<byte[]>
{
    public string? Type { get;  set; }
    public Guid? FatherId { get; set; }
    public bool? IsActive { get; set; }
}

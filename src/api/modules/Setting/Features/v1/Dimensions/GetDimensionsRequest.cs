using FSH.Framework.Core.Paging;
using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public class GetDimensionsRequest : BaseFilter, IRequest<List<DimensionDto>>
{
    public string? Type { get;  set; }
    public Guid? FatherId { get; set; }
    public bool? IsActive { get; set; }
}

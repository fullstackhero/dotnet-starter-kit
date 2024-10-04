using Ardalis.Specification;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Setting.Domain;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public sealed class ExportEntityCodesSpecs : EntitiesByBaseFilterSpec<EntityCode, EntityCodeDto>
{
    public ExportEntityCodesSpecs(ExportEntityCodesRequest request)
        : base(request) =>
            Query
                .Where(e => e.Type.Equals(request.Type!.Value), request.Type.HasValue)
                    .OrderBy(e => e.Order);
}

using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public class ExportDimensionsHandler(
    [FromKeyedServices("setting:dimension")]  IReadRepository<Dimension> repository, IDataExport dataExport)
    : IRequestHandler<ExportDimensionsRequest, byte[]>
{
    public async Task<byte[]> Handle(ExportDimensionsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = new ExportDimensionsSpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        
        return dataExport.ListToByteArray(items);
    }
}

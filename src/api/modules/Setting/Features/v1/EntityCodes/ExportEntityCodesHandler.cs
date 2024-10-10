using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public class ExportEntityCodesHandler(
    [FromKeyedServices("setting:EntityCode")]  IReadRepository<EntityCode> repository, IDataExport dataExport)
    : IRequestHandler<ExportEntityCodesRequest, byte[]>
{
    public async Task<byte[]> Handle(ExportEntityCodesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = new ExportEntityCodesSpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        
        return dataExport.ListToByteArray(items);
    }
}

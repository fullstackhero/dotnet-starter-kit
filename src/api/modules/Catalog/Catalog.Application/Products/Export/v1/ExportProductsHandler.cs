using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Export.v1;

public class ExportProductsHandler(
    [FromKeyedServices("catalog:products")]  IReadRepository<Product> repository, IDataExport dataExport)
    : IRequestHandler<ExportProductsRequest, byte[]>
{
    public async Task<byte[]> Handle(ExportProductsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = new ExportProductsSpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        
        return dataExport.ListToByteArray(items);
    }
}

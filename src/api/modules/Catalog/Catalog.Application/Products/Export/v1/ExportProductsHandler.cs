using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Products.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using FSH.Starter.WebApi.Catalog.Application.Products.Search.v1;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Export.v1;

public class ExportProductsHandler(
    [FromKeyedServices("catalog:products")]  IReadRepository<Product> repository, IDataExport dataExport)
    : IRequestHandler<ExportProductsRequest, byte[]>
{
    public async Task<byte[]> Handle(ExportProductsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var spec = new EntitiesByBaseFilterSpec<Product, ProductDto>(request.Filter);
        var items = await repository.ListAsync(spec, cancellationToken);
        
        return dataExport.ListToByteArray(items);
    }
}

using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Products.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Export.v1;

public class ExportProductsHandler(
    [FromKeyedServices("catalog:products")]  IReadRepository<Product> repository, IExcelWriter excelWriter)
    : IRequestHandler<ExportProductsCommand, Stream>
{
    public async Task<Stream> Handle(ExportProductsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var spec = new EntitiesByBaseFilterSpec<Product, ProductResponse>(request.Filter);
        var items = await repository.ListAsync(spec, cancellationToken);
        
        var response = excelWriter.WriteToStream(items);
        
        return response;
    }
}

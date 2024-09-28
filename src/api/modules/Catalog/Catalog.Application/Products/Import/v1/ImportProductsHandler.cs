using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Storage.File;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Catalog.Application.Products.Import.v1;

public class ImportProductsHandler(
    [FromKeyedServices("catalog:products")]  IRepository<Product> repository, IDataImport dataImport)
    : IRequestHandler<ImportProductsCommand, int>
{
    public async Task<int> Handle(ImportProductsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var items = await dataImport.ToListAsync<Product>(request.UploadFile, FileType.Excel);

        if (items == null || items.Count == 0) throw new CustomException("Excel file error or empty!");

        try
        {
            await repository.UpdateRangeAsync(items, cancellationToken);
        }
        catch (Exception)
        {
            throw new CustomException("Internal error!");
        }

        return items.Count;
    }
}

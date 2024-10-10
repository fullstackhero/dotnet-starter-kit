using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Storage.File;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public class ImportEntityCodesHandler(
    [FromKeyedServices("setting:EntityCode")]  IRepository<EntityCode> repository, IDataImport dataImport)
    : IRequestHandler<ImportEntityCodesCommand, ImportResponse>
{
    public async Task<ImportResponse> Handle(ImportEntityCodesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var items = await dataImport.ToListAsync<EntityCode>(request.UploadFile, FileType.Excel);
        
        ImportResponse response = new()
        {
            TotalRecords = items.Count, 
            Message = ""
    
        };

        if (response.TotalRecords <= 0)
        {
            response.Message = "File is empty or Invalid format";
            return response;
        }
            
        try
        {
            if (request.IsUpdate)
            {
                await repository.UpdateRangeAsync(items, cancellationToken);
                response.Message = " Updated successful";
            }
            else
            {
                await repository.AddRangeAsync (items, cancellationToken);
                response.Message = "Added successful";
            }
        }
        catch (Exception)
        {
            response.Message = "Internal error!";
            // throw new CustomException("Internal error!")
        }

        return response;
    }
}

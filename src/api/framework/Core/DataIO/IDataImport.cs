using FSH.Framework.Core.Storage.File;
using FSH.Framework.Core.Storage.File.Features;

namespace FSH.Framework.Core.DataIO;

public interface IDataImport
{
    
    Task<IList<T>> ToListAsync<T>(FileUploadCommand request, FileType supportedFileType, string sheetName = "Sheet1");
}
